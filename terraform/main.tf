terraform {
  backend "azurerm" {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "atlas.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

locals {
  repository_name     = "Atlas"
  environment         = var.ENVIRONMENT
  location            = var.LOCATION
  min_tls_version     = "1.0"
  resource_group_name = "${local.environment}-ATLAS-RESOURCE-GROUP"
  subscription_id     = var.AZURE_SUBSCRIPTION_ID
  common_tags = {
    controlled_by_terraform = true
    repository_name         = local.repository_name
  }
}

provider "azurerm" {
  version         = "1.28.0"
  subscription_id = local.subscription_id

  // According to the docs, the default behaviour is to attempt to register every possible resource provider
  // whether or not we actually need it. See docs of this property for more context.
  // However, registering providers requires higher permissions than general development, so attempting to do
  // so will trigger 403s for most devs. Accordingly, we disable the "register everything" behaviour, and
  // initial registrations will need to be organised as a one-off.
  // Currently, the only resource provider needed is this AzureRM provider.
  skip_provider_registration = false
}

module "matching_algorithm" {
  source = "./modules/matching_algorithm"

  general = {
    environment     = local.environment
    location        = local.location
    subscription_id = local.subscription_id
    common_tags     = local.common_tags
  }

  default_servicebus_settings = local.service-bus

  app_service_plan        = azurerm_app_service_plan.atlas
  sql_server              = azurerm_sql_server.atlas_sql_server
  shared_function_storage = azurerm_storage_account.shared_function_storage
  azure_storage           = azurerm_storage_account.azure_storage
  application_insights    = azurerm_application_insights.atlas
  servicebus_namespace    = azurerm_servicebus_namespace.general

  servicebus_namespace_authorization_rules = {
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    updated-searchable-donors = azurerm_servicebus_topic.updated-searchable-donors
    alerts                    = azurerm_servicebus_topic.alerts
    notifications             = azurerm_servicebus_topic.notifications
  }

  APPLICATION_INSIGHTS_LOG_LEVEL                   = var.APPLICATION_INSIGHTS_LOG_LEVEL
  AZURE_CLIENT_ID                                  = var.AZURE_CLIENT_ID
  AZURE_CLIENT_SECRET                              = var.AZURE_CLIENT_SECRET
  DONOR_SERVICE_APIKEY                             = var.DONOR_SERVICE_APIKEY
  DONOR_SERVICE_BASEURL                            = var.DONOR_SERVICE_BASEURL
  DONOR_SERVICE_READ_DONORS_FROM_FILE              = var.DONOR_SERVICE_READ_DONORS_FROM_FILE
  HLA_SERVICE_APIKEY                               = var.HLA_SERVICE_APIKEY
  HLA_SERVICE_BASEURL                              = var.HLA_SERVICE_BASEURL
  DATA_REFRESH_DB_SIZE_ACTIVE                      = var.MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE
  DATA_REFRESH_DB_SIZE_DORMANT                     = var.MATCHING_DATA_REFRESH_DB_SIZE_DORMANT
  DATA_REFRESH_DB_SIZE_REFRESH                     = var.MATCHING_DATA_REFRESH_DB_SIZE_REFRESH
  DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME          = var.MATCHING_DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME
  DATA_REFRESH_CRONTAB                             = var.MATCHING_DATA_REFRESH_CRONTAB
  DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS = var.MATCHING_DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS
  DATABASE_PASSWORD                                = var.MATCHING_DATABASE_PASSWORD
  DATABASE_USERNAME                                = var.MATCHING_DATABASE_USERNAME
  FUNCTION_HOST_KEY                                = var.MATCHING_FUNCTION_HOST_KEY
  MESSAGING_BUS_DONOR_BATCH_SIZE                   = var.MATCHING_MESSAGING_BUS_DONOR_BATCH_SIZE
  MESSAGING_BUS_DONOR_CRON_SCHEDULE                = var.MATCHING_MESSAGING_BUS_DONOR_CRON_SCHEDULE
  WEBSITE_RUN_FROM_PACKAGE                         = var.WEBSITE_RUN_FROM_PACKAGE
  WMDA_FILE_URL                                    = var.WMDA_FILE_URL
}
