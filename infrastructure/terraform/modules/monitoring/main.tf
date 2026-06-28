variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "prefix" { type = string }
variable "tags" { type = map(string) }
variable "api_app_service_id" { type = string }
variable "ai_app_service_id" { type = string }
variable "monthly_budget" { type = number }

# ─── Log Analytics Workspace ───────────────────────────────────
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${var.prefix}-logs"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30 # Free: 5GB/month ingestion
  tags                = var.tags
}

# ─── Application Insights ─────────────────────────────────────
resource "azurerm_application_insights" "main" {
  name                = "${var.prefix}-insights"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  retention_in_days   = 30
  tags                = var.tags
}

# ─── Budget Alert ──────────────────────────────────────────────
resource "azurerm_consumption_budget_resource_group" "main" {
  name              = "${var.prefix}-budget"
  resource_group_id = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/resourceGroups/${var.resource_group_name}"

  amount     = var.monthly_budget
  time_grain = "Monthly"

  time_period {
    start_date = formatdate("YYYY-MM-01'T'00:00:00Z", timestamp())
  }

  notification {
    enabled   = true
    threshold = 80
    operator  = "GreaterThanOrEqualTo"
    contact_emails = []
  }

  notification {
    enabled   = true
    threshold = 100
    operator  = "GreaterThanOrEqualTo"
    contact_emails = []
  }

  lifecycle {
    ignore_changes = [time_period]
  }
}

data "azurerm_subscription" "current" {}

# ─── Alerts ────────────────────────────────────────────────────
resource "azurerm_monitor_metric_alert" "api_5xx" {
  name                = "${var.prefix}-api-5xx"
  resource_group_name = var.resource_group_name
  scopes              = [var.api_app_service_id]
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  tags                = var.tags

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5
  }
}

resource "azurerm_monitor_metric_alert" "api_response_time" {
  name                = "${var.prefix}-api-latency"
  resource_group_name = var.resource_group_name
  scopes              = [var.api_app_service_id]
  severity            = 3
  frequency           = "PT5M"
  window_size         = "PT15M"
  tags                = var.tags

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "HttpResponseTime"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 10 # seconds
  }
}

output "instrumentation_key" {
  value     = azurerm_application_insights.main.instrumentation_key
  sensitive = true
}
output "workspace_id" { value = azurerm_log_analytics_workspace.main.id }
