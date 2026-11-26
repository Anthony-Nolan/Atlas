locals {
  // The largest max_size_gb that each tier the data refresh can scale a transient matching database to allows.
  // Keep in step with Atlas.MatchingAlgorithm/Models/AzureManagement/AzureDatabaseSize.cs.
  // Sources: https://learn.microsoft.com/azure/azure-sql/database/resource-limits-dtu-single-databases
  // and https://learn.microsoft.com/azure/azure-sql/database/resource-limits-vcore-single-databases
  //
  // The data refresh changes the tier without changing the max size, and Azure refuses a tier whose limit is below the
  // max size. So MATCHING_DATABASE_MAX_SIZE_GB must fit the refresh, active and dormant tiers alike.
  matching_database_tier_max_size_gb = {
    Basic = 2

    S0  = 250
    S1  = 250
    S2  = 250
    S3  = 1024
    S4  = 1024
    S6  = 1024
    S7  = 1024
    S9  = 1024
    S12 = 1024

    P1  = 1024
    P2  = 1024
    P4  = 1024
    P6  = 1024
    P11 = 4096
    P15 = 4096

    GP_S_Gen5_1  = 512
    GP_S_Gen5_2  = 1024
    GP_S_Gen5_4  = 1024
    GP_S_Gen5_6  = 1024
    GP_S_Gen5_8  = 2048
    GP_S_Gen5_10 = 2048
    GP_S_Gen5_12 = 3072
    GP_S_Gen5_14 = 3072
    GP_S_Gen5_16 = 3072
    GP_S_Gen5_18 = 3072
    GP_S_Gen5_20 = 3072
    GP_S_Gen5_24 = 4096
    GP_S_Gen5_32 = 4096
    GP_S_Gen5_40 = 4096
  }

  matching_data_refresh_db_sizes = {
    MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE  = var.MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE
    MATCHING_DATA_REFRESH_DB_SIZE_DORMANT = var.MATCHING_DATA_REFRESH_DB_SIZE_DORMANT
    MATCHING_DATA_REFRESH_DB_SIZE_REFRESH = var.MATCHING_DATA_REFRESH_DB_SIZE_REFRESH
  }
}
