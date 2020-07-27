// TODO: ATLAS-314: finish off and test this terraform code
// This code has not been tested, it was dropped mid way through when deemed out of scope of another ticket.
// It has been deemed likely enough to be useful in ATLAS-314 that it has been left in regardless

// TODO: ATLAS-314: Parameterise backend details 
// See non-test terraform for examples.
terraform {
  backend "azurerm" {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "atlas.systemtests.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

provider "azurerm" {
  version         = "2.11.0"
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

