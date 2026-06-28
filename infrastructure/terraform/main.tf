terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "kv-tfstate-rg"
    storage_account_name = "kvtfstate"
    container_name       = "tfstate"
    key                  = "knowledgevault.tfstate"
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

locals {
  prefix = "kv-${var.environment}"
  tags = {
    Project     = "KnowledgeVault"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "${local.prefix}-rg"
  location = var.location
  tags     = local.tags
}

module "acr" {
  source              = "./modules/acr"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
}

module "database" {
  source              = "./modules/database"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
  admin_password      = var.db_admin_password
}

module "redis" {
  source              = "./modules/redis"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
}

module "storage" {
  source              = "./modules/storage"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
}

module "app_service" {
  source              = "./modules/app-service"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
  acr_login_server    = module.acr.login_server
  acr_admin_username  = module.acr.admin_username
  acr_admin_password  = module.acr.admin_password
  database_url        = module.database.connection_string
  redis_url           = module.redis.connection_string
  storage_connection  = module.storage.connection_string
  ai_engine_url       = "https://${local.prefix}-ai.azurewebsites.net"
}

module "monitoring" {
  source              = "./modules/monitoring"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  prefix              = local.prefix
  tags                = local.tags
  api_app_service_id  = module.app_service.api_app_id
  ai_app_service_id   = module.app_service.ai_app_id
  monthly_budget      = var.monthly_budget
}
