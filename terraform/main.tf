terraform {
  backend "azurerm" {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "search-algorithm.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

data terraform_remote_state nova_core {
  backend   = "azurerm"
  workspace = "${terraform.workspace}"

  config = {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "core.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

locals {
  environment         = "${data.terraform_remote_state.nova_core.outputs.general.environment}"
  location            = "${data.terraform_remote_state.nova_core.outputs.nova_resource_group.location}"
  resource_group_name = "${data.terraform_remote_state.nova_core.outputs.nova_resource_group.name}"
  min_tls_version     = "${data.terraform_remote_state.nova_core.outputs.general.min_tls_version}"
  cors_urls           = "${data.terraform_remote_state.nova_core.outputs.general.cors_urls}"
}

provider "azurerm" {
  version         = "1.28.0"
  subscription_id = "${data.terraform_remote_state.nova_core.outputs.general.subscription_id}"
}
