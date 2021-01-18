// ReSharper disable InconsistentNaming
namespace Atlas.MatchingAlgorithm.Models.AzureManagement
{
    /// <summary>
    /// Standard/Premium Options documented here: https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-dtu-single-databases
    /// VCore/Serverless Options documented here: https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-vcore-single-databases
    /// 
    /// Note that while there appear to be gaps in the sequences, each tier is exhaustive - some tier numbers do not exist in Azure. 
    /// </summary>
    public enum AzureDatabaseSize
    {
        // Basic
        Basic,
        
        // Standard
        S0,
        S1,
        S2,
        S3,
        S4,
        S6,
        S7,
        S9,
        S12,
        
        // Premium
        P1,
        P2,
        P4,
        P6,
        P11,
        P15,
        
        // Serverless
        GP_S_Gen5_1,
        GP_S_Gen5_2,
        GP_S_Gen5_4,
        GP_S_Gen5_6,
        GP_S_Gen5_8,
        GP_S_Gen5_10,
        GP_S_Gen5_12,
        GP_S_Gen5_14,
        GP_S_Gen5_16,
        GP_S_Gen5_18,
        GP_S_Gen5_20,
        GP_S_Gen5_24,
        GP_S_Gen5_32,
        GP_S_Gen5_40,
    }
}