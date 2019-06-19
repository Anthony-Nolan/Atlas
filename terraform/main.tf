terraform {
  backend "azurerm" {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "search-algorithm.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

locals {
  repository_name                    = "Nova.SearchAlgorithm"
  environment                        = data.terraform_remote_state.nova_core.outputs.general.environment
  location                           = data.terraform_remote_state.nova_core.outputs.nova_resource_group.location
  resource_group_name                = data.terraform_remote_state.nova_core.outputs.nova_resource_group.name
  min_tls_version                    = data.terraform_remote_state.nova_core.outputs.general.min_tls_version
  cors_urls                          = data.terraform_remote_state.nova_core.outputs.general.cors_urls
  function_storage_connection_string = data.terraform_remote_state.nova_core.outputs.shared_function_storage.primary_connection_string
  common_tags                        = {
    controlled_by_terraform = true
    environment             = local.environment
    repository_name         = local.repository_name
  }
}

provider "azurerm" {
  version         = "1.28.0"
  subscription_id = data.terraform_remote_state.nova_core.outputs.general.subscription_id
}
