resource "azurerm_resource_group" "atlas_system_tests_resource_group" {
  location = local.location
  name     = local.resource_group_name

  tags = local.common_tags

  lifecycle {
    prevent_destroy = true
  }
}