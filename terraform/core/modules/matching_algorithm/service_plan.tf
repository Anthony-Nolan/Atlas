// deploy matching algorithm to different service plan to control maximum worker count independently of top level functions
// matching algorithm cannot be allowed to scale as much as match prediction, due to database concurrency limits
resource "azurerm_app_service_plan" "atlas-matching-algorithm-elastic-plan" {
  name                = "${var.general.environment}-ATLAS-MATCHING-ELASTIC-PLAN"
  location            = var.general.location
  resource_group_name = var.resource_group.name
  kind                = "elastic"
  // maximum running instances of the algorithm = maximum_worker_count * maxConcurrentCalls (in host.json).
  // together these must ensure that the number of allowed concurrent SQL connections to the matching SQL DB is not exceeded.
  // Note that this is 200 workers for an S3 plan, and that each algorithm invocation can open up to 4 concurrent connections.
  maximum_elastic_worker_count = 10

  sku {
    tier = "ElasticPremium"
    size = "EP1"
  }
}