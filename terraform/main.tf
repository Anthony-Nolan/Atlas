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
  common_tags         = {
    controlled_by_terraform = true
    repository_name         = local.repository_name
  }
  data_refresh_a_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-data-refresh-a.name};Persist Security Info=False;User ID=${azurerm_sql_server.atlas_sql_server.administrator_login};Password=${azurerm_sql_server.atlas_sql_server.administrator_login_password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  data_refresh_b_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-data-refresh-b.name};Persist Security Info=False;User ID=${azurerm_sql_server.atlas_sql_server.administrator_login};Password=${azurerm_sql_server.atlas_sql_server.administrator_login_password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  data_refresh_persistent_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-persistent.name};Persist Security Info=False;User ID=${azurerm_sql_server.atlas_sql_server.administrator_login};Password=${azurerm_sql_server.atlas_sql_server.administrator_login_password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

provider "azurerm" {
  version         = "1.28.0"
  subscription_id = "6114522f-eea5-44ab-94ab-af37ffffc4d3"
}
