data "terraform_remote_state" "atlas" {
  backend   = "azurerm"
  workspace = terraform.workspace

  config = {
    container_name       = var.TERRAFORM_STORAGE_CONTAINER_NAME
    key                  = var.TERRAFORM_KEY
    resource_group_name  = var.TERRAFORM_RESOURCE_GROUP_NAME
    storage_account_name = var.TERRAFORM_STORAGE_ACCOUNT_NAME
  }
}