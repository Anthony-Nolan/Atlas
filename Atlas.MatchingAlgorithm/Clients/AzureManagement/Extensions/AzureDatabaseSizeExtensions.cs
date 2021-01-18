using System;
using System.Dynamic;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.Extensions
{
    public static class AzureDatabaseSizeExtensions
    {
        private const string BasicTier = "Basic";
        private const string StandardTier = "Standard";
        private const string PremiumTier = "Premium";
        
        // Used for "Serverless" tier
        private const string GeneralPurposeTier = "GeneralPurpose";

        public static string ToAzureApiUpdateBody(this AzureDatabaseSize databaseSize)
        {
            string tier;
            int? capacity = null;
            string name = null;

            switch (databaseSize)
            {
                case AzureDatabaseSize.Basic:
                    tier = BasicTier;
                    break;
                case AzureDatabaseSize.S0:
                    tier = StandardTier;
                    capacity = 10;
                    break;
                case AzureDatabaseSize.S1:
                    tier = StandardTier;
                    capacity = 20;
                    break;
                case AzureDatabaseSize.S2:
                    tier = StandardTier;
                    capacity = 50;
                    break;
                case AzureDatabaseSize.S3:
                    tier = StandardTier;
                    capacity = 100;
                    break;
                case AzureDatabaseSize.S4:
                    tier = StandardTier;
                    capacity = 200;
                    break;
                case AzureDatabaseSize.S6:
                    tier = StandardTier;
                    capacity = 400;
                    break;
                case AzureDatabaseSize.S7:
                    tier = StandardTier;
                    capacity = 800;
                    break;
                case AzureDatabaseSize.S9:
                    tier = StandardTier;
                    capacity = 1600;
                    break;
                case AzureDatabaseSize.S12:
                    tier = StandardTier;
                    capacity = 3000;
                    break;
                case AzureDatabaseSize.P1:
                    tier = PremiumTier;
                    capacity = 125;
                    break;
                case AzureDatabaseSize.P2:
                    tier = PremiumTier;
                    capacity = 250;
                    break;
                case AzureDatabaseSize.P4:
                    tier = PremiumTier;
                    capacity = 500;
                    break;
                case AzureDatabaseSize.P6:
                    tier = PremiumTier;
                    capacity = 1000;
                    break;
                case AzureDatabaseSize.P11:
                    tier = PremiumTier;
                    capacity = 1750;
                    break;
                case AzureDatabaseSize.P15:
                    tier = PremiumTier;
                    capacity = 4000;
                    break;
                case AzureDatabaseSize.GP_S_Gen5_1:
                case AzureDatabaseSize.GP_S_Gen5_2:
                case AzureDatabaseSize.GP_S_Gen5_4:
                case AzureDatabaseSize.GP_S_Gen5_6:
                case AzureDatabaseSize.GP_S_Gen5_8:
                case AzureDatabaseSize.GP_S_Gen5_10:
                case AzureDatabaseSize.GP_S_Gen5_12:
                case AzureDatabaseSize.GP_S_Gen5_14:
                case AzureDatabaseSize.GP_S_Gen5_16:
                case AzureDatabaseSize.GP_S_Gen5_18:
                case AzureDatabaseSize.GP_S_Gen5_20:
                case AzureDatabaseSize.GP_S_Gen5_24:
                case AzureDatabaseSize.GP_S_Gen5_32:
                case AzureDatabaseSize.GP_S_Gen5_40:
                    tier = GeneralPurposeTier;
                    name = databaseSize.ToString();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseSize), databaseSize, null);
            }

            dynamic skuObject = new ExpandoObject();
            skuObject.tier = tier;
            skuObject.capacity = capacity;
            skuObject.name = name;

            dynamic body = new ExpandoObject();
            body.sku = skuObject;

            return JsonConvert.SerializeObject(body);
        }
    }
}