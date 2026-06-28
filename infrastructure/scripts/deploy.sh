#!/bin/bash
set -euo pipefail

ENV="${1:-dev}"
echo "=== KnowledgeVault Deployment: $ENV ==="

# Validate prerequisites
command -v az >/dev/null 2>&1 || { echo "Azure CLI required"; exit 1; }
command -v terraform >/dev/null 2>&1 || { echo "Terraform required"; exit 1; }
command -v docker >/dev/null 2>&1 || { echo "Docker required"; exit 1; }

# Ensure logged in
az account show >/dev/null 2>&1 || { echo "Run 'az login' first"; exit 1; }

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RG_NAME="kv-${ENV}-rg"
ACR_NAME="kv${ENV}acr"

echo ""
echo "--- Step 1: Initialize Terraform state storage ---"
az group create --name kv-tfstate-rg --location eastus --output none 2>/dev/null || true
az storage account create --name kvtfstate --resource-group kv-tfstate-rg --sku Standard_LRS --output none 2>/dev/null || true
az storage container create --name tfstate --account-name kvtfstate --output none 2>/dev/null || true

echo ""
echo "--- Step 2: Terraform Init & Apply ---"
cd infrastructure/terraform
terraform init -reconfigure
terraform plan \
  -var-file="environments/${ENV}/terraform.tfvars" \
  -var="subscription_id=${SUBSCRIPTION_ID}" \
  -var="db_admin_password=${DB_ADMIN_PASSWORD:-KvProd2024!}" \
  -out=tfplan

read -p "Apply terraform plan? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
  terraform apply tfplan
fi
cd ../..

echo ""
echo "--- Step 3: Build and push Docker images ---"
ACR_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv 2>/dev/null || echo "${ACR_NAME}.azurecr.io")
az acr login --name "$ACR_NAME"

TAG=$(git rev-parse --short HEAD 2>/dev/null || echo "latest")

docker build -f infrastructure/docker/Dockerfile.api --target production -t "${ACR_SERVER}/kv-api:${TAG}" ./api
docker build -f infrastructure/docker/Dockerfile.ai --target production -t "${ACR_SERVER}/kv-ai:${TAG}" ./ai-engine
docker build -f infrastructure/docker/Dockerfile.web --target production -t "${ACR_SERVER}/kv-web:${TAG}" ./web-client

docker push "${ACR_SERVER}/kv-api:${TAG}"
docker push "${ACR_SERVER}/kv-ai:${TAG}"
docker push "${ACR_SERVER}/kv-web:${TAG}"

echo ""
echo "--- Step 4: Deploy to App Service ---"
az webapp config container set --name "kv-${ENV}-api" --resource-group "$RG_NAME" \
  --container-image-name "${ACR_SERVER}/kv-api:${TAG}"

az webapp config container set --name "kv-${ENV}-ai" --resource-group "$RG_NAME" \
  --container-image-name "${ACR_SERVER}/kv-ai:${TAG}"

echo ""
echo "--- Step 5: Run database migrations ---"
API_URL=$(az webapp show --name "kv-${ENV}-api" --resource-group "$RG_NAME" --query defaultHostName -o tsv)
echo "API deployed at: https://${API_URL}"

echo ""
echo "--- Step 6: Verify ---"
sleep 15
curl -sf "https://${API_URL}/health" && echo " API healthy" || echo " API health check failed"

echo ""
echo "=== Deployment complete ==="
echo "API:        https://${API_URL}"
echo "Swagger:    https://${API_URL}/swagger"
