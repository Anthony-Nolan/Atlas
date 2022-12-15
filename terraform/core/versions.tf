terraform {
  required_version = ">= 0.15, < 0.16"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.39.0"
    }
  }
}
