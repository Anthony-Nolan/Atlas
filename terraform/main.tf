// TODO: ATLAS-324: Parameterise backend details
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

  app_service_plan          = azurerm_app_service_plan.atlas
  donor_import_sql_database = module.donor_import.sql_database
  sql_server                = azurerm_sql_server.atlas_sql_server
  function_storage          = azurerm_storage_account.function_storage
  azure_storage             = azurerm_storage_account.azure_storage
  application_insights      = azurerm_application_insights.atlas
  servicebus_namespace      = azurerm_servicebus_namespace.general

  servicebus_namespace_authorization_rules = {
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    updated-searchable-donors = module.donor_import.general.updated_searchable_donors_servicebus_topic
    alerts                    = module.support.general.alerts_servicebus_topic
    notifications             = module.support.general.notifications_servicebus_topic
  }

  APPLICATION_INSIGHTS_LOG_LEVEL                   = var.APPLICATION_INSIGHTS_LOG_LEVEL
  AZURE_CLIENT_ID                                  = var.AZURE_CLIENT_ID
  AZURE_CLIENT_SECRET                              = var.AZURE_CLIENT_SECRET
  DATA_REFRESH_DB_SIZE_ACTIVE                      = var.MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE
  DATA_REFRESH_DB_SIZE_DORMANT                     = var.MATCHING_DATA_REFRESH_DB_SIZE_DORMANT
  DATA_REFRESH_DB_SIZE_REFRESH                     = var.MATCHING_DATA_REFRESH_DB_SIZE_REFRESH
  DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME          = var.MATCHING_DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME
  DATA_REFRESH_CRONTAB                             = var.MATCHING_DATA_REFRESH_CRONTAB
  DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS = var.MATCHING_DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS
  DATABASE_PASSWORD                                = var.MATCHING_DATABASE_PASSWORD
  DATABASE_USERNAME                                = var.MATCHING_DATABASE_USERNAME
  DONOR_IMPORT_DATABASE_PASSWORD                   = var.MATCHING_PASSWORD_FOR_DONOR_IMPORT_DATABASE
  DONOR_IMPORT_DATABASE_USERNAME                   = var.MATCHING_USERNAME_FOR_DONOR_IMPORT_DATABASE
  FUNCTION_HOST_KEY                                = var.MATCHING_FUNCTION_HOST_KEY
  HLA_SERVICE_APIKEY                               = var.HLA_SERVICE_APIKEY
  HLA_SERVICE_BASEURL                              = var.HLA_SERVICE_BASEURL
  MESSAGING_BUS_DONOR_BATCH_SIZE                   = var.MATCHING_MESSAGING_BUS_DONOR_BATCH_SIZE
  MESSAGING_BUS_DONOR_CRON_SCHEDULE                = var.MATCHING_MESSAGING_BUS_DONOR_CRON_SCHEDULE
  WEBSITE_RUN_FROM_PACKAGE                         = var.WEBSITE_RUN_FROM_PACKAGE
  WMDA_FILE_URL                                    = var.WMDA_FILE_URL
}

module "match_prediction" {
  source = "./modules/match_prediction"

  general = {
    environment = local.environment
    location    = local.location
    common_tags = local.common_tags
  }

  app_service_plan     = azurerm_app_service_plan.atlas
  sql_server           = azurerm_sql_server.atlas_sql_server
  function_storage     = azurerm_storage_account.function_storage
  azure_storage        = azurerm_storage_account.azure_storage
  application_insights = azurerm_application_insights.atlas


  servicebus_namespace_authorization_rules = {
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    alerts        = module.support.general.alerts_servicebus_topic
    notifications = module.support.general.notifications_servicebus_topic
  }

  APPLICATION_INSIGHTS_LOG_LEVEL = var.APPLICATION_INSIGHTS_LOG_LEVEL
  DATABASE_PASSWORD              = var.MATCH_PREDICTION_DATABASE_PASSWORD
  DATABASE_USERNAME              = var.MATCH_PREDICTION_DATABASE_USERNAME
  WEBSITE_RUN_FROM_PACKAGE       = var.WEBSITE_RUN_FROM_PACKAGE
}

module "donor_import" {
  source = "./modules/donor_import"

  general = {
    environment = local.environment
    location    = local.location
    common_tags = local.common_tags
  }

  default_servicebus_settings = local.service-bus

  app_service_plan     = azurerm_app_service_plan.atlas
  application_insights = azurerm_application_insights.atlas
  azure_storage        = azurerm_storage_account.azure_storage
  function_storage     = azurerm_storage_account.function_storage
  resource_group       = azurerm_resource_group.atlas_resource_group
  servicebus_namespace = azurerm_servicebus_namespace.general
  sql_server           = azurerm_sql_server.atlas_sql_server

  servicebus_namespace_authorization_rules = {
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    alerts        = module.support.general.alerts_servicebus_topic
    notifications = module.support.general.notifications_servicebus_topic
  }

  APPLICATION_INSIGHTS_LOG_LEVEL = var.APPLICATION_INSIGHTS_LOG_LEVEL
  DATABASE_PASSWORD              = var.DONOR_DATABASE_PASSWORD
  DATABASE_USERNAME              = var.DONOR_DATABASE_USERNAME
  FUNCTIONS_MASTER_KEY           = var.DONOR_IMPORT_FUNCTION_MASTER_KEY
}

module "support" {
  source = "./modules/support"

  default_servicebus_settings = local.service-bus

  app_service_plan     = azurerm_app_service_plan.atlas
  servicebus_namespace = azurerm_servicebus_namespace.general
}

module "multiple_allele_code_lookup" {
  source = "./modules/multiple_allele_code_lookup"

  app_service_plan = azurerm_app_service_plan.atlas
  azure_storage    = azurerm_storage_account.azure_storage
}