variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "prefix" { type = string }
variable "tags" { type = map(string) }
variable "acr_login_server" { type = string }
variable "acr_admin_username" { type = string }
variable "acr_admin_password" { type = string sensitive = true }
variable "database_url" { type = string sensitive = true }
variable "redis_url" { type = string sensitive = true }
variable "storage_connection" { type = string sensitive = true }
variable "ai_engine_url" { type = string }

# ─── App Service Plan (F1 Free Tier) ───────────────────────────
resource "azurerm_service_plan" "main" {
  name                = "${var.prefix}-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "F1" # Free: 60 CPU-min/day, 1GB RAM
  tags                = var.tags
}

# B1 plan for AI engine (F1 doesn't support containers well)
resource "azurerm_service_plan" "ai" {
  name                = "${var.prefix}-ai-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "B1" # Basic: $13/mo, 1.75GB RAM
  tags                = var.tags
}

# ─── .NET API App Service ──────────────────────────────────────
resource "azurerm_linux_web_app" "api" {
  name                = "${var.prefix}-api"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true
  tags                = var.tags

  site_config {
    always_on                         = false # Required for F1
    container_registry_use_managed_identity = false
    application_stack {
      docker_image_name   = "knowledgevault-api:latest"
      docker_registry_url = "https://${var.acr_login_server}"
    }
    health_check_path = "/health"
  }

  app_settings = {
    "DOCKER_REGISTRY_SERVER_URL"      = "https://${var.acr_login_server}"
    "DOCKER_REGISTRY_SERVER_USERNAME" = var.acr_admin_username
    "DOCKER_REGISTRY_SERVER_PASSWORD" = var.acr_admin_password
    "ConnectionStrings__PostgreSQL"    = var.database_url
    "Redis__ConnectionString"         = var.redis_url
    "AIEngine__BaseUrl"               = var.ai_engine_url
    "ASPNETCORE_ENVIRONMENT"          = "Production"
    "WEBSITES_PORT"                   = "5000"
  }

  identity {
    type = "SystemAssigned"
  }
}

# ─── Python AI Engine App Service ──────────────────────────────
resource "azurerm_linux_web_app" "ai_engine" {
  name                = "${var.prefix}-ai"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.ai.id
  https_only          = true
  tags                = var.tags

  site_config {
    always_on = true
    application_stack {
      docker_image_name   = "knowledgevault-ai:latest"
      docker_registry_url = "https://${var.acr_login_server}"
    }
    health_check_path = "/health"
  }

  app_settings = {
    "DOCKER_REGISTRY_SERVER_URL"      = "https://${var.acr_login_server}"
    "DOCKER_REGISTRY_SERVER_USERNAME" = var.acr_admin_username
    "DOCKER_REGISTRY_SERVER_PASSWORD" = var.acr_admin_password
    "OLLAMA_BASE_URL"                 = "http://localhost:11434"
    "QDRANT_HOST"                     = "localhost"
    "REDIS_URL"                       = var.redis_url
    "WEBSITES_PORT"                   = "8000"
  }

  identity {
    type = "SystemAssigned"
  }
}

# ─── Static Web App for React Frontend ─────────────────────────
resource "azurerm_static_web_app" "web" {
  name                = "${var.prefix}-web"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = var.tags
}

output "api_url" { value = "https://${azurerm_linux_web_app.api.default_hostname}" }
output "ai_url" { value = "https://${azurerm_linux_web_app.ai_engine.default_hostname}" }
output "web_url" { value = "https://${azurerm_static_web_app.web.default_host_name}" }
output "api_app_id" { value = azurerm_linux_web_app.api.id }
output "ai_app_id" { value = azurerm_linux_web_app.ai_engine.id }
