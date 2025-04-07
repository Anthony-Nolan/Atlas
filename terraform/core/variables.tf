variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type        = string
  default     = "Info"
  description = "Corresponds to the severity levels defined by application insights. Allowed values: Verbose, Info (maps to Information), Warn (maps to Warning), Error, Critical."
}

variable "AZURE_CLIENT_ID" {
  type        = string
  description = "Client ID used for authenticating to manage Azure resources from code."
}

variable "AZURE_CLIENT_SECRET" {
  type        = string
  description = "Client secret used for authenticating to manage Azure resources from code."
}

variable "AZURE_OAUTH_BASEURL" {
  type        = string
  description = "Base Url used for authenticating with Azure via OAuth, when managing Azure resources from code. Expected to be of form: 'https://login.microsoftonline.com/<domain>/oauth2/v2.0/token'"
}

variable "AZURE_TENANT_ID" {
  type        = string
  description = "Tenant id used for authenticating with Azure via OAuth, when querying log analytics workspace from code. Expected to be domain name or GUID"
}

variable "AZURE_SUBSCRIPTION_ID" {
  type        = string
  description = "ID of the Azure subscription into which the system will be deployed."
}

variable "DATABASE_SERVER_ADMIN_LOGIN" {
  type    = string
  default = "atlas-admin"
}

variable "DATABASE_SERVER_ADMIN_LOGIN_PASSWORD" {
  type = string
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_LOGIN_USERNAME" {
  type        = string
  description = "Name of the AD group used to control admin access to the SQL server."
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_OBJECTID" {
  type        = string
  description = "Object ID of admin access AD group."
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_TENANTID" {
  type        = string
  description = "ID of Tenant where admin access AD group resides."
}

variable "DATABASE_SHARED_EDITION" {
  type        = string
  default     = "Standard"
  description = "Database edition. Defaults to 'Standard' for SKU (S0, P1, etc.) pricing model. For VCore (including Serverless) model, must be changed to 'GeneralPurpose'"
}

variable "DATABASE_SHARED_MAX_SIZE_GB" {
  type        = string
  default     = "30"
  description = "Maximum size in gigabytes, refer to Azure documentation for supported sizes."
}

variable "DATABASE_SHARED_SKU_SIZE" {
  type        = string
  default     = "S2"
  description = "Appropriate values determined by DATABASE_SHARED_EDITION. Defaults to 'Standard', where only standard sku sizes are appropriate e.g. S0, S2, S3. Updating to 'GeneralPurpose' instead allows VCore/Serverless tiers, e.g. GP_S_Gen5_2"
}

variable "DONOR_DATABASE_PASSWORD" {
  type = string
}

variable "DONOR_DATABASE_USERNAME" {
  type    = string
  default = "donors"
}

variable "DONOR_ID_CHECKER_RESULTS_SUBSCRIPTION_NAMES" {
  type        = list(string)
  default     = []
  description = "Subscription names for the donor-id-checker-results Service Bus topic (in addition to Audit subscription). If not provided, no additional subscriptions will be created."
}

variable "DONOR_INFO_CHECKER_RESULTS_SUBSCRIPTION_NAMES" {
  type        = list(string)
  default     = []
  description = "Subscription names for the donor-info-checker-results Service Bus topic (in addition to Audit subscription). If not provided, no additional subscriptions will be created."
}

variable "DONOR_IMPORT_DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB" {
  type        = string
  default     = "0 0 0 * * *"
  description = "Crontab used to determine how often to delete expired, published donor updates."
}

variable "DONOR_IMPORT_MAX_INSTANCES" {
  type        = number
  default     = 1
  description = "Determines the number of instances of the donor import that can exist in parallel. Combined with the per-instance service bus concurrency settings in host.json, defines the maximum number of concurrent instances of the donor import. Can be larger with a larger database."
}

variable "DONOR_IMPORT_NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR" {
  type        = bool
  default     = true
  description = "When true, notifications will be sent on every attempt to delete a donor that does not exist in Atlas"
}

variable "DONOR_IMPORT_PUBLISH_DONOR_UPDATES_CRONTAB" {
  type        = string
  default     = "0 * * * * *"
  description = "Crontab used to determine how often to check for and publish new donor updates."
}

variable "DONOR_IMPORT_PUBLISHED_UPDATE_EXPIRY_IN_DAYS" {
  type        = number
  default     = 30
  description = "Number of days after publishing that a donor update will expire and be eligible for deletion."
}

variable "DONOR_IMPORT_STALLED_FILE_CHECK_CRONTAB" {
  type    = string
  default = "0 */30 * * * *"
}

variable "DONOR_IMPORT_STALLED_FILE_DURATION" {
  type        = string
  default     = "2"
  description = "How long, in hours, a file must have been in the 'Started' state to be considered stalled"
}

variable "DONOR_IMPORT_ALLOW_FULL_MODE_IMPORT" {
  type        = bool
  default     = false
  description = "Controls whether Full mode donor import files will be accepted (true) or rejected (false)"
}

variable "DONOR_IMPORT_FAILURE_LOGS_CRONTAB" {
  type        = string
  default     = "0 0 0 * * *"
  description = "Crontab used to determine how often to delete expired donor import failure logs."
}

variable "DONOR_IMPORT_FAILURE_LOGS_EXPIRY_IN_DAYS" {
  type        = number
  default     = 60
  description = "Number of days after donor import failure logs will expire and be eligible for deletion."
}

variable "ELASTIC_SERVICE_PLAN_MAX_SCALE_OUT" {
  type        = number
  default     = 50
  description = "The maximum number of workers that can be scaled out on the service plan. Affects all functions apps - which can be further restricted, but can never exceed this limit."
}

variable "ELASTIC_SERVICE_PLAN_MAX_SCALE_OUT_PUBLIC_API" {
  type        = number
  default     = 10
  description = "The maximum number of workers that can be scaled out on the service plan for public functions api."
}

variable "ELASTIC_SERVICE_PLAN_SKU_SIZE" {
  type        = string
  default     = "EP1"
  description = "This database will be on the Elastic Premium tier, so only elastic premium sku sizes are appropriate e.g. EP1, EP2, EP3. Each tier represents a double in service plan price, and a corresponding halving of algorithm time."
}

variable "ELASTIC_SERVICE_PLAN_FOR_PUBLIC_API" {
  type        = bool
  default     = true
  description = "Should there be a separate elastic service plan for the public API functions app? If false, the public API will be hosted on the same elastic service plan as all other functions."
}

variable "ENVIRONMENT" {
  type        = string
  description = "Prepended to all ATLAS resources, to indicate which environment of the installation they represent. Some alphanumeric characters must be present, as non-alphanumeric characters will be stripped from the storage account name. Max 8 alphanumeric characters. e.g. DEV/UAT/LIVE"
}

// Note that there is another optional parameter, "subnet_id", allowed in function app ip_restriction blocks.
// However, terraform does not yet allow optional parameters in an object variable, so we have removed it - see https://github.com/hashicorp/terraform/issues/19898
variable "IP_RESTRICTION_SETTINGS" {
  type        = list(string)
  default     = []
  description = "List of IP addresses that are whitelisted for functions app access. If none are provided the resources will publicly available. Note that if using this feature, Azure Devops build agents will need to be whitelisted for setting up webhooks - follow the Azure documentation on how to find said IP ranges. Warning that they change weekly - as such a lot of maintenance would be necessary to use whitelisted function apps, and as such using this feature is not recommended."
}

variable "LOCATION" {
  type        = string
  default     = "uksouth"
  description = "GeoLocation of all Azure resources for this ATLAS installation."
}

variable "LOG_ANALYTICS_DAILY_QUOTA_GB" {
  type        = number
  default     = -1
  description = "The Log Analytics workspace daily quota for ingestion in GB. Default is -1 (unlimited)."
}

variable "LOG_ANALYTICS_SKU" {
  type        = string
  default     = "PerGB2018"
  description = "Log Analytics Workspace SKU. Default is Pay As You Go."
}

variable "MAC_IMPORT_CRON_SCHEDULE" {
  type        = string
  default     = "0 0 2 * * *"
  description = "Crontab used to determine when to run the ImportMacs Function."
}

variable "MAC_SOURCE" {
  type        = string
  default     = "https://hml.nmdp.org/mac/files/alpha.v3.zip"
  description = "The source of our Multiple Allele Codes"
}

variable "MATCH_PREDICTION_DATABASE_PASSWORD" {
  type = string
}

variable "MATCH_PREDICTION_DATABASE_USERNAME" {
  type    = string
  default = "match_prediction"
}

variable "MATCH_PREDICTION_DOWNLOAD_BATCH_SIZE" {
  type        = number
  default     = 10
  description = "Batch size for downloading match prediction results"
}

variable "MATCHING_PREDICTION_PROCESSING_BATCH_SIZE" {
  type        = number
  default     = 1000
  description = "Batch size for processing match prediction requests. Batching will not be used if the value is less than or equal to 0."
}

variable "MATCHING_BATCH_SIZE" {
  type        = number
  default     = 250000
  description = "Batch size at which donors will be iterated in the matching algorithm. Larger = quicker, but higher memory footprint."
}

variable "MATCHING_DATA_REFRESH_AUTO_RUN" {
  type        = bool
  default     = true
  description = "When set, the data refresh job to update processed donor data will automatically run when a new nomenclature version is detected. Otherwise, it will be manual only."
}

variable "MATCHING_MAINTENANCE_GCCOLLECT_DISABLED" {
  type        = bool
  default     = true
  description = "Specifies whether forced garbage collection is enabled (required for sliding cache to take effect faster)"
}

variable "MATCHING_MAINTENANCE_GCCOLLECT_CRON_SCHEDULE" {
  type        = string
  default     = "*/30 * * * * *"
  description = "Specifies the schedule of the forced garbage collection (required for sliding cache to take effect faster)"
}

variable "MATCHING_MAX_CONCURRENT_PROCESSES_PER_INSTANCE" {
  type        = number
  default     = 3
  description = "The maximum number of concurrent search requests that can run on each instance of the matching algorithm."
}

variable "MATCHING_MAX_SCALE_OUT" {
  type        = number
  default     = 3
  description = "The maximum number of instances of the matching algorithm that can be scaled out."
}

variable "MATCHING_DATABASE_TRANSIENT_TIMEOUT" {
  type        = number
  default     = 1800
  description = "The timeout to be used in the connection string for the transient matching database. The default is half an hour - which may not be enough for particularly large ATLAS installations."
}


variable "MATCHING_DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE" {
  type        = number
  default     = -1
  description = "The 'auto-pause' duration for the active matching database, in minutes. Only relevant for serverless database tier - will be ignored for other tiers. -1 = auto-pause disabled. Minimum 60."
}

variable "MATCHING_DATA_REFRESH_DB_AUTO_PAUSE_DORMANT" {
  type        = number
  default     = -1
  description = "The 'auto-pause' duration for the dormant matching database, in minutes. Only relevant for serverless database tier - will be ignored for other tiers. -1 = auto-pause disabled. Minimum 60."
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE" {
  type        = string
  default     = "S4"
  description = "Size of Azure Database used for active matching database. Allowed values according to the Azure DTU/GeneralPurpose Serverless model service tiers."
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_DORMANT" {
  type        = string
  default     = "S0"
  description = "Size of Azure Database used for dormant matching database. Allowed values according to the Azure DTU/GeneralPurpose Serverless model service tiers."
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_REFRESH" {
  type        = string
  default     = "P1"
  description = "Size to temporarily scale the dormant Azure Database to, whilst refreshing the matching database. Allowed values according to the Azure DTU model service tiers. Premium tier is recommended due to a large IO throughput."
}

variable "MATCHING_DATA_REFRESH_CRONTAB" {
  type        = string
  default     = "0 0 0 * * Monday"
  description = "A crontab determining when the matching data refresh will be auto-attempted. It will only run to completion if new HLA nomenclature is detected."
}

variable "MATCHING_DATABASE_MAX_SIZE_GB" {
  type        = string
  default     = "250"
  description = "Maximum size in gigabytes, refer to Azure documentation for supported sizes"
}

variable "MATCHING_DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS" {
  type        = string
  default     = "1000"
  description = "When scaling matching database from code, how long to wait between polling Azure for an updated status."
}

variable "MATCHING_DATABASE_PASSWORD" {
  type = string
}

variable "MATCHING_DATABASE_USERNAME" {
  type    = string
  default = "matching"
}

variable "MATCHING_DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH" {
  type        = bool
  default     = false
  description = "Should the Write for a Donor be entirely Transactional when running DataRefresh. 'false' for greater performance. 'true' for greater reliability"
}

variable "MATCHING_DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES" {
  type        = bool
  default     = true
  description = "Should the Write for a Donor be entirely Transactional when running DataRefresh. 'false' for greater performance. 'true' for greater reliability"
}

variable "MATCHING_MESSAGING_BUS_DONOR_BATCH_SIZE" {
  type        = number
  default     = 350
  description = "Batch size used for ongoing donor updates to the matching component."
}

variable "MATCHING_MESSAGING_BUS_DONOR_CRON_SCHEDULE" {
  type        = string
  default     = "0 */1 * * * *"
  description = "Crontab used to determine when to poll for new batches of donor updates to the matching component."
}

variable "MATCHING_PASSWORD_FOR_DONOR_IMPORT_DATABASE" {
  type = string
}

variable "MATCHING_USERNAME_FOR_DONOR_IMPORT_DATABASE" {
  type    = string
  default = "matchingForDonorSchema"
}

variable "MAX_CONCURRENT_ACTIVITY_FUNCTIONS" {
  type        = number
  default     = 1
  description = "Maximum number of concurrent activity functions in the top level Atlas functions app. Notably affects match prediction concurrency per-instance."
}

variable "ORCHESTRATION_MATCH_PREDICTION_BATCH_SIZE" {
  type    = number
  default = 10
}

variable "REPEAT_SEARCH_DATABASE_PASSWORD" {
  type = string
}

variable "REPEAT_SEARCH_DATABASE_USERNAME" {
  type    = string
  default = "repeat_search"
}

variable "REPEAT_SEARCH_MATCHING_MAX_CONCURRENT_PROCESSES_PER_INSTANCE" {
  type        = number
  default     = 1
  description = "The maximum number of concurrent repeat search requests that can run on each instance of the matching algorithm."
}

variable "REPEAT_SEARCH_MATCHING_MAX_SCALE_OUT" {
  type        = number
  default     = 1
  description = "The maximum number of instances of the repeat search's matching algorithm that can be scaled out."
}

variable "RESULTS_BATCH_SIZE" {
  type        = number
  default     = 0
  description = "Batch size (number of results written per file) for saving search/matching results"
}

variable "SEARCH_RESULTS_READY_SUBSCRIPTION_NAMES" {
  type        = list(string)
  default     = []
  description = "Subscription names for the search-results-ready and repeat-search-results-ready Service Bus topics (in addition to Audit subscriptions). If not provided, no additional subscriptions will be created."
}

variable "SEARCH_TRACKING_DATABASE_PASSWORD" {
  type = string
}

variable "SEARCH_TRACKING_DATABASE_USERNAME" {
  type    = string
  default = "search_tracking"
}

variable "SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS" {
	type        = number
	default     = 20
	description = "When sending a service bus message, time to wait before retrying a failed message"
}

variable "SERVICE_BUS_SEND_RETRY_COUNT" {
	type        = number
	default     = 5
	description = "When sending a service bus message, the total number of retries to attempt"
}

variable "SHOULD_BATCH_RESULTS" {
  type        = bool
  default     = false
  description = "Inidicates whether final search/repeat search results should be batched or not"
}

variable "TERRAFORM_RESOURCE_GROUP_NAME" {
  type        = string
  description = "Resource group in which the terraform backend is deployed."
}

variable "TERRAFORM_STORAGE_ACCOUNT_NAME" {
  type        = string
  description = "Name of the storage account in which the terraform backend is deployed."
}

variable "TERRAFORM_STORAGE_CONTAINER_NAME" {
  type        = string
  description = "Name of the container within the storage account in which the terraform backend is deployed."
}

variable "WMDA_FILE_URL" {
  type        = string
  default     = "https://raw.githubusercontent.com/ANHIG/IMGTHLA/"
  description = "A URL hosting HLA nomenclature in the expected format."
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type    = string
  default = "1"
}

variable "ATLAS_FUNCTION_SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
  default  = null
}

variable "MATCHING_ALGORITHM_SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
  default  = null
}

variable "MATCH_PREDICTION_SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
  default  = null
}

variable "REPEAT_SEARCH_SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
  default  = null
}

variable "SUPPORT_DEADLETTER_ALERTS_ACTION_GROUP_ID" {
  type        = string
  default     = null
  description = "The ID of the action group to be used for deadletter alerts."
}
