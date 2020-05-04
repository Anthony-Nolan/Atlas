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
}
