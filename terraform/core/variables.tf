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

variable "DATABASE_SHARED_EDITION" {
  type        = string
  default     = "Standard"
  description = "Database edition. Defaults to 'Standard' for SKU (S0, P1, etc.) pricing model. For VCore (including Serverless) model, must be changed to 'GeneralPurpose'"
}

variable "DATABASE_SHARED_MAX_SIZE" {
  type        = string
  default     = "32212254720"
  description = "Maximum size in bytes, refer to Azure documentation for supported sizes."
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

variable "DONOR_IMPORT_NOTIFICATIONS_ON_SUCCESSFUL_IMPORT" {
  type        = bool
  default     = true
  description = "When true, notifications will be sent on every successful donor file import"
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

variable "ELASTIC_SERVICE_PLAN_SKU_SIZE" {
  type        = string
  default     = "EP1"
  description = "This database will be on the Elastic Premium tier, so only elastic premium sku sizes are appropriate e.g. EP1, EP2, EP3. Each tier represents a double in service plan price, and a corresponding halving of algorithm time."
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

variable "MAC_IMPORT_CRON_SCHEDULE" {
  type        = string
  default     = "0 0 2 * * *"
  description = "Crontab used to determine when to run the ImportMacs Function."
}

variable "MAC_SOURCE" {
  type        = string
  default     = "https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip"
  description = "The source of our Multiple Allele Codes"
}

variable "MATCH_PREDICTION_DATABASE_PASSWORD" {
  type = string
}

variable "MATCH_PREDICTION_DATABASE_USERNAME" {
  type    = string
  default = "match_prediction"
}

variable "MATCH_PREDICTION_SUPPRESS_COMPRESSED_PHENOTYPE_CONVERSION_EXCEPTIONS" {
  type        = bool
  default     = false
  description = "Compressed phenotype conversion exceptions should NOT be suppressed when running match prediction requests outside of search."
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

variable "MATCHING_DATABASE_MAX_SIZE" {
  type        = string
  default     = "268435456000"
  description = "Maximum size in bytes, refer to Azure documentation for supported sizes"
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

variable "MATCHING_FUNCTION_HOST_KEY" {
  type        = string
  default     = ""
  description = "Optional. Host keys cannot be set from terraform. This should be set up manually, and is only included to be used as an export. If unset, other terraformed apps cannot use the ATLAS remote state to fetch the host key, and must have it provided manually."
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

variable "PUBLIC_API_FUNCTION_HOST_KEY" {
  type        = string
  default     = ""
  description = "Optional. Host keys cannot be set from terraform. This should be set up manually, and is only included to be used as an export. If unset, other terraformed apps cannot use the ATLAS remote state to fetch the host key, and must have it provided manually."
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

variable "SEARCH_SUPPRESS_COMPRESSED_PHENOTYPE_CONVERSION_EXCEPTIONS" {
  type        = bool
  default     = true
  description = "Compressed phenotype conversion exceptions should be suppressed when running match prediction as part of search."
}

variable "SERVICE_PLAN_MAX_SCALE_OUT" {
  type        = number
  default     = 50
  description = "The maximum number of workers that can be scaled out on the service plan. Affects all functions apps - which can be further restricted, but can never exceed this limit."
}

variable "SERVICE_PLAN_SKU" {
  type = object({
    tier = string,
    size = string
  })
  default = {
    tier = "Standard"
    size = "S1"
  }
  description = "The SKU size for the *non-elastic* service plan. This only hosts the donor import functions, all other services live on the elastic plan."
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
