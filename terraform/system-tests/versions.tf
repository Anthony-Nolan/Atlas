terraform {
  required_version = ">= 1.4, < 2.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.36.0"
    }
  }
}
