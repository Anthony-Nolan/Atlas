data "external" "function_keys" {
  program = ["pwsh", "${local.fetch_key_ps1}"]

  query = {
    functionAppId = "${var.function_app_id}"
    clientId = "${var.client_id}"
    clientSecret = "${var.client_secret}"
  }
}