variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "prefix" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_redis_cache" "main" {
  name                          = "${var.prefix}-redis"
  resource_group_name           = var.resource_group_name
  location                      = var.location
  capacity                      = 0
  family                        = "C"
  sku_name                      = "Basic" # 250MB, free tier eligible
  non_ssl_port_enabled          = false
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true
  tags                          = var.tags

  redis_configuration {
    maxmemory_policy = "allkeys-lru"
  }
}

output "connection_string" {
  value     = "${azurerm_redis_cache.main.hostname}:${azurerm_redis_cache.main.ssl_port},password=${azurerm_redis_cache.main.primary_access_key},ssl=True,abortConnect=False"
  sensitive = true
}
output "host" { value = azurerm_redis_cache.main.hostname }
