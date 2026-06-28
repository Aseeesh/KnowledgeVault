variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "prefix" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_storage_account" "main" {
  name                     = replace("${var.prefix}storage", "-", "")
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS" # Cheapest option
  access_tier              = "Hot"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags

  blob_properties {
    delete_retention_policy {
      days = 7
    }
  }
}

resource "azurerm_storage_container" "documents" {
  name                  = "documents"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "exports" {
  name                  = "exports"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Queue for ingestion (replaces RabbitMQ in cloud)
resource "azurerm_storage_queue" "ingestion" {
  name                 = "document-ingestion"
  storage_account_name = azurerm_storage_account.main.name
}

output "connection_string" {
  value     = azurerm_storage_account.main.primary_connection_string
  sensitive = true
}
output "account_name" { value = azurerm_storage_account.main.name }
output "documents_container" { value = azurerm_storage_container.documents.name }
