terraform {
  backend "azurerm" {
    key = "atlas.terraform.tfstate"
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
  subscription_id = local.subscription_id

  // According to the docs, the default behaviour is to attempt to register every possible resource provider
  // whether or not we actually need it. See docs of this property for more context.
  // However, registering providers requires higher permissions than general development, so attempting to do
  // so will trigger 403s for most devs. Accordingly, we disable the "register everything" behaviour, and
  // initial registrations will need to be organised as a one-off.
  // Currently, the only resource provider needed is this AzureRM provider.
  skip_provider_registration = false
  features {}
}

module "donor_import" {
  source = "./modules/donor_import"

  general = {
    environment = local.environment
    location    = local.location
    common_tags = local.common_tags
  }

  default_servicebus_settings = local.service-bus

  // DI Variables 
  app_service_plan        = azurerm_service_plan.atlas-elastic-plan
  application_insights    = azurerm_application_insights.atlas
  azure_storage           = azurerm_storage_account.azure_storage
  servicebus_namespace    = azurerm_servicebus_namespace.general
  shared_function_storage = azurerm_storage_account.function_storage
  sql_database            = azurerm_mssql_database.atlas-database-shared
  sql_server              = azurerm_mssql_server.atlas_sql_server

  servicebus_namespace_authorization_rules = {
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
  }

  servicebus_topics = {
    alerts        = module.support.general.alerts_servicebus_topic
    notifications = module.support.general.notifications_servicebus_topic
  }

  // Release variables
  APPLICATION_INSIGHTS_LOG_LEVEL             = var.APPLICATION_INSIGHTS_LOG_LEVEL
  DATABASE_PASSWORD                          = var.DONOR_DATABASE_PASSWORD
  DATABASE_USERNAME                          = var.DONOR_DATABASE_USERNAME
  DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB     = var.DONOR_IMPORT_DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB
  IP_RESTRICTION_SETTINGS                    = var.IP_RESTRICTION_SETTINGS
  MAX_INSTANCES                              = var.DONOR_IMPORT_MAX_INSTANCES
  NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR = var.DONOR_IMPORT_NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR
  NOTIFICATIONS_ON_SUCCESSFUL_IMPORT         = var.DONOR_IMPORT_NOTIFICATIONS_ON_SUCCESSFUL_IMPORT
  PUBLISH_DONOR_UPDATES_CRONTAB              = var.DONOR_IMPORT_PUBLISH_DONOR_UPDATES_CRONTAB
  PUBLISHED_UPDATE_EXPIRY_IN_DAYS            = var.DONOR_IMPORT_PUBLISHED_UPDATE_EXPIRY_IN_DAYS
  STALLED_FILE_CHECK_CRONTAB                 = var.DONOR_IMPORT_STALLED_FILE_CHECK_CRONTAB
  STALLED_FILE_DURATION                      = var.DONOR_IMPORT_STALLED_FILE_DURATION
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

  // DI variables
  application_insights      = azurerm_application_insights.atlas
  azure_storage             = azurerm_storage_account.azure_storage
  donor_import_sql_database = azurerm_mssql_database.atlas-database-shared
  elastic_app_service_plan  = azurerm_service_plan.atlas-elastic-plan
  mac_import_table          = module.multiple_allele_code_lookup.storage_table
  resource_group            = azurerm_resource_group.atlas_resource_group
  servicebus_namespace      = azurerm_servicebus_namespace.general
  shared_function_storage   = azurerm_storage_account.function_storage
  sql_database_shared       = azurerm_mssql_database.atlas-database-shared
  sql_server                = azurerm_mssql_server.atlas_sql_server

  servicebus_namespace_authorization_rules = {
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    updated-searchable-donors = module.donor_import.service_bus.updated_searchable_donors_topic
    alerts                    = module.support.general.alerts_servicebus_topic
    notifications             = module.support.general.notifications_servicebus_topic
  }

  // Release variables
  APPLICATION_INSIGHTS_LOG_LEVEL                   = var.APPLICATION_INSIGHTS_LOG_LEVEL
  AZURE_CLIENT_ID                                  = var.AZURE_CLIENT_ID
  AZURE_CLIENT_SECRET                              = var.AZURE_CLIENT_SECRET
  AZURE_OAUTH_BASEURL                              = var.AZURE_OAUTH_BASEURL
  DATA_REFRESH_AUTO_RUN                            = var.MATCHING_DATA_REFRESH_AUTO_RUN
  DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE                = var.MATCHING_DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE
  DATA_REFRESH_DB_AUTO_PAUSE_DORMANT               = var.MATCHING_DATA_REFRESH_DB_AUTO_PAUSE_DORMANT
  DATA_REFRESH_DB_SIZE_ACTIVE                      = var.MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE
  DATA_REFRESH_DB_SIZE_DORMANT                     = var.MATCHING_DATA_REFRESH_DB_SIZE_DORMANT
  DATA_REFRESH_DB_SIZE_REFRESH                     = var.MATCHING_DATA_REFRESH_DB_SIZE_REFRESH
  DATA_REFRESH_CRONTAB                             = var.MATCHING_DATA_REFRESH_CRONTAB
  DATABASE_MAX_SIZE_GB                             = var.MATCHING_DATABASE_MAX_SIZE_GB
  DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS = var.MATCHING_DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS
  DATABASE_PASSWORD                                = var.MATCHING_DATABASE_PASSWORD
  DATABASE_TRANSIENT_TIMEOUT                       = var.MATCHING_DATABASE_TRANSIENT_TIMEOUT
  DATABASE_USERNAME                                = var.MATCHING_DATABASE_USERNAME
  DONOR_IMPORT_DATABASE_PASSWORD                   = var.MATCHING_PASSWORD_FOR_DONOR_IMPORT_DATABASE
  DONOR_IMPORT_DATABASE_USERNAME                   = var.MATCHING_USERNAME_FOR_DONOR_IMPORT_DATABASE
  DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH       = var.MATCHING_DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH
  DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES      = var.MATCHING_DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES
  IP_RESTRICTION_SETTINGS                          = var.IP_RESTRICTION_SETTINGS
  MATCHING_BATCH_SIZE                              = var.MATCHING_BATCH_SIZE
  MAX_CONCURRENT_SERVICEBUS_FUNCTIONS              = var.MATCHING_MAX_CONCURRENT_PROCESSES_PER_INSTANCE
  MAX_SCALE_OUT                                    = var.MATCHING_MAX_SCALE_OUT
  MESSAGING_BUS_DONOR_BATCH_SIZE                   = var.MATCHING_MESSAGING_BUS_DONOR_BATCH_SIZE
  MESSAGING_BUS_DONOR_CRON_SCHEDULE                = var.MATCHING_MESSAGING_BUS_DONOR_CRON_SCHEDULE
  RESULTS_BATCH_SIZE                               = var.RESULTS_BATCH_SIZE
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
  default_servicebus_settings = local.service-bus

  // DI Variables
  application_insights    = azurerm_application_insights.atlas
  app_service_plan        = azurerm_service_plan.atlas-elastic-plan
  azure_storage           = azurerm_storage_account.azure_storage
  servicebus_namespace    = azurerm_servicebus_namespace.general
  shared_function_storage = azurerm_storage_account.function_storage
  sql_server              = azurerm_mssql_server.atlas_sql_server
  sql_database            = azurerm_mssql_database.atlas-database-shared
  mac_import_table        = module.multiple_allele_code_lookup.storage_table

  servicebus_namespace_authorization_rules = {
    manage     = azurerm_servicebus_namespace_authorization_rule.manage
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }

  servicebus_topics = {
    alerts        = module.support.general.alerts_servicebus_topic
    notifications = module.support.general.notifications_servicebus_topic
  }

  // Release variables
  APPLICATION_INSIGHTS_LOG_LEVEL = var.APPLICATION_INSIGHTS_LOG_LEVEL
  DATABASE_PASSWORD              = var.MATCH_PREDICTION_DATABASE_PASSWORD
  DATABASE_USERNAME              = var.MATCH_PREDICTION_DATABASE_USERNAME
  IP_RESTRICTION_SETTINGS        = var.IP_RESTRICTION_SETTINGS
  MAC_SOURCE                     = var.MAC_SOURCE
  WEBSITE_RUN_FROM_PACKAGE       = var.WEBSITE_RUN_FROM_PACKAGE
}

module "multiple_allele_code_lookup" {
  source = "./modules/multiple_allele_code_lookup"

  azure_storage = azurerm_storage_account.azure_storage
}

module "repeat_search" {
  source = "./modules/repeat_search"

  general = {
    environment = local.environment
    location    = local.location
    common_tags = local.common_tags
  }
  default_servicebus_settings = local.service-bus

  // DI Variables
  application_insights                            = azurerm_application_insights.atlas
  app_service_plan                                = azurerm_service_plan.atlas-elastic-plan
  azure_storage                                   = azurerm_storage_account.azure_storage
  donor_database_connection_string                = module.donor_import.sql_database.connection_string
  mac_import_table                                = module.multiple_allele_code_lookup.storage_table
  matching_persistent_database_connection_string  = module.matching_algorithm.sql_database.persistent_database_connection_string
  matching_transient_a_database_connection_string = module.matching_algorithm.sql_database.transient_a_database_connection_string
  matching_transient_b_database_connection_string = module.matching_algorithm.sql_database.transient_b_database_connection_string
  original-search-matching-results-topic          = module.matching_algorithm.service_bus.matching_results_topic
  servicebus_namespace                            = azurerm_servicebus_namespace.general
  servicebus_namespace_authorization_rules = {
    read-write = azurerm_servicebus_namespace_authorization_rule.read-write
    read-only  = azurerm_servicebus_namespace_authorization_rule.read-only
    write-only = azurerm_servicebus_namespace_authorization_rule.write-only
  }
  servicebus_topics = {
    alerts        = module.support.general.alerts_servicebus_topic
    notifications = module.support.general.notifications_servicebus_topic
  }
  shared_function_storage = azurerm_storage_account.function_storage
  sql_database            = azurerm_mssql_database.atlas-database-shared
  sql_server              = azurerm_mssql_server.atlas_sql_server


  // Release variables
  APPLICATION_INSIGHTS_LOG_LEVEL      = var.APPLICATION_INSIGHTS_LOG_LEVEL
  DATABASE_PASSWORD                   = var.REPEAT_SEARCH_DATABASE_PASSWORD
  DATABASE_USERNAME                   = var.REPEAT_SEARCH_DATABASE_USERNAME
  IP_RESTRICTION_SETTINGS             = var.IP_RESTRICTION_SETTINGS
  MATCHING_BATCH_SIZE                 = var.MATCHING_BATCH_SIZE
  MAX_CONCURRENT_SERVICEBUS_FUNCTIONS = var.REPEAT_SEARCH_MATCHING_MAX_CONCURRENT_PROCESSES_PER_INSTANCE
  MAX_SCALE_OUT                       = var.REPEAT_SEARCH_MATCHING_MAX_SCALE_OUT
  RESULTS_BATCH_SIZE                  = var.RESULTS_BATCH_SIZE
}

module "support" {
  source = "./modules/support"

  default_servicebus_settings = local.service-bus

  resource_group       = azurerm_resource_group.atlas_resource_group
  servicebus_namespace = azurerm_servicebus_namespace.general
}
