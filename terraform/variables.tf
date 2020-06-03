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

variable "DONOR_DATABASE_PASSWORD" {
  type = string
}

variable "DONOR_DATABASE_USERNAME" {
  type    = string
  default = "donors"
}

variable "DONOR_SERVICE_APIKEY" {
  type = string
}

variable "DONOR_SERVICE_BASEURL" {
  type = string
}

variable "DONOR_SERVICE_READ_DONORS_FROM_FILE" {
  type        = bool
  default     = false
  description = "When set to 'true', will read donor details from an EmbeddedResource file rather than attempting to contact the Donor Service."
}

variable "ENVIRONMENT" {
  type        = string
  description = "Prepended to all ATLAS resources, to indicate which environment of the installation they represent. Some alphanumeric characters must be present, as non-alphanumeric characters will be stripped from the storage account name. Max 8 alphanumeric characters. e.g. DEV/UAT/LIVE"
}

variable "HLA_SERVICE_APIKEY" {
  type = string
}

variable "HLA_SERVICE_BASEURL" {
  type = string
}

variable "LOCATION" {
  type        = string
  default     = "uksouth"
  description = "GeoLocation of all Azure resources for this ATLAS installation."
}

variable "MAC_SOURCE" {
  type = string 
  default = "https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip"
  description = "The source of our Multiple Allele Codes"
}

variable "MATCH_PREDICTION_DATABASE_PASSWORD" {
  type = string
}

variable "MATCH_PREDICTION_DATABASE_USERNAME" {
  type    = string
  default = "match_prediction"
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE" {
  type        = string
  default     = "S4"
  description = "Size of Azure Database used for active matching database. Allowed values according to the Azure DTU model service tiers."
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_DORMANT" {
  type        = string
  default     = "S0"
  description = "Size of Azure Database used for dormant matching database. Allowed values according to the Azure DTU model service tiers."
}

variable "MATCHING_DATA_REFRESH_DB_SIZE_REFRESH" {
  type        = string
  default     = "P1"
  description = "Size to temproarily scale the dormant Azure Database to, whilst refreshing the matching database. Allowed values according to the Azure DTU model service tiers. Premium tier is recommended due to a large IO throughput."
}

variable "MATCHING_DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME" {
  type        = string
  default     = "ManageDonorByAvailability"
  description = "Name of the donor import function that should be disabled during the full matching data refresh."
}

variable "MATCHING_DATA_REFRESH_CRONTAB" {
  type        = string
  default     = "0 0 0 * * Monday"
  description = "A crontab determining when the matching data refresh will be auto-attempted. It will only run to completion if new HLA nomenclature is detected."
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

variable "SERVICE_PLAN_SKU" {
  type = object({
    tier = string,
    size = string
  })
  default = {
    tier = "Standard"
    size = "S1"
  }
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
