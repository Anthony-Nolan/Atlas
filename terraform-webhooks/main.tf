terraform {
  // TODO: ATLAS-324: Do not hard code nova values
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
