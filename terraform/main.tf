terraform {
  backend "azurerm" {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "search-algorithm.terraform.tfstate"
    resource_group_name  = "ANAZRG01"
  }
}

data terraform_remote_state nova_core {
  backend   = "azurerm"
  workspace = "${terraform.workspace}"

  config {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "core.terraform.tfstate"
    resource_group_name  = "ANAZRG01"
  }
}

locals {
  environment         = "${data.terraform_remote_state.nova_core.general.environment}"
  location            = "${data.terraform_remote_state.nova_core.nova_resource_group.location}"
  resource_group_name = "${data.terraform_remote_state.nova_core.nova_resource_group.name}"
  min_tls_version     = "${data.terraform_remote_state.nova_core.general.min_tls_version}"
  cors_urls           = "${data.terraform_remote_state.nova_core.general.cors_urls}"
}

provider "azurerm" {
  version         = "1.25.0"
  subscription_id = "${data.terraform_remote_state.nova_core.general.subscription_id}"
}
