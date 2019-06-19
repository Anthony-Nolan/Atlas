data "terraform_remote_state" "nova_core" {
  backend   = "azurerm"
  workspace = terraform.workspace

  config = {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "core.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

data "terraform_remote_state" "hla" {
  backend   = "azurerm"
  workspace = terraform.workspace

  config = {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "hla.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}

data "terraform_remote_state" "donor" {
  backend   = "azurerm"
  workspace = terraform.workspace

  config = {
    storage_account_name = "novaterraform"
    container_name       = "terraform-state"
    key                  = "donor.terraform.tfstate"
    resource_group_name  = "AN-RESOURCE-GROUP"
  }
}