terraform {
  backend "azurerm" {
    key = "atlas.systemtests.terraform.tfstate"
  }
}

provider "azurerm" {
  version         = "2.39.0"
  subscription_id = var.AZURE_SUBSCRIPTION_ID

  // According to the docs, the default behaviour is to attempt to register every possible resource provider
  // whether or not we actually need it. See docs of this property for more context.
  // However, registering providers requires higher permissions than general development, so attempting to do
  // so will trigger 403s for most devs. Accordingly, we disable the "register everything" behaviour, and
  // initial registrations will need to be organised as a one-off.
  // Currently, the only resource provider needed is this AzureRM provider.
  skip_provider_registration = false
  features {}
}

locals {
  repository_name     = "Atlas"
  location            = "uksouth"
  min_tls_version     = "1.0"
  resource_group_name = "ATLAS-SYSTEM-TEST-RESOURCE-GROUP"
  common_tags = {
    controlled_by_terraform = true
    repository_name         = local.repository_name
  }
}

