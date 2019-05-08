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

provider "azurerm" {
  version         = "1.25.0"
  subscription_id = "${data.terraform_remote_state.nova_core.general.subscription_id}"
}

resource "azurerm_app_service_plan" "search_algorithm" {
  name                = "${data.terraform_remote_state.nova_core.general.environment}-SEARCH-ALGORITHM"
  location            = "${data.terraform_remote_state.nova_core.nova_resource_group.location}"
  resource_group_name = "${data.terraform_remote_state.nova_core.nova_resource_group.name}"

  sku = {
    tier = "${var.service-plan-sku["tier"]}"
    size = "${var.service-plan-sku["size"]}"
  }
}
