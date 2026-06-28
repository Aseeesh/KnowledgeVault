variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "prefix" { type = string }
variable "tags" { type = map(string) }
variable "admin_password" { type = string sensitive = true }

resource "azurerm_postgresql_flexible_server" "main" {
  name                          = "${var.prefix}-pgserver"
  resource_group_name           = var.resource_group_name
  location                      = var.location
  version                       = "16"
  administrator_login           = "kvadmin"
  administrator_password        = var.admin_password
  storage_mb                    = 32768 # 32GB free tier
  sku_name                      = "B_Standard_B1ms"
  backup_retention_days         = 7
  geo_redundant_backup_enabled  = false
  public_network_access_enabled = true
  tags                          = var.tags

  zone = "1"
}

resource "azurerm_postgresql_flexible_server_database" "main" {
  name      = "knowledgevault"
  server_id = azurerm_postgresql_flexible_server.main.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

output "host" { value = azurerm_postgresql_flexible_server.main.fqdn }
output "connection_string" {
  value     = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Port=5432;Database=knowledgevault;Username=kvadmin;Password=${var.admin_password};SSL Mode=Require"
  sensitive = true
}
