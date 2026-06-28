output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "acr_login_server" {
  value = module.acr.login_server
}

output "api_url" {
  value = module.app_service.api_url
}

output "ai_engine_url" {
  value = module.app_service.ai_url
}

output "web_client_url" {
  value = module.app_service.web_url
}

output "database_host" {
  value     = module.database.host
  sensitive = true
}

output "storage_account_name" {
  value = module.storage.account_name
}

output "app_insights_key" {
  value     = module.monitoring.instrumentation_key
  sensitive = true
}
