using System;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models.AzureManagement;

namespace Nova.SearchAlgorithm.Clients.AzureManagement.Extensions
{
    public static class AzureDatabaseSizeExtensions
    {
        public static string ToAzureApiUpdateBody(this AzureDatabaseSize databaseSize)
        {
            string name;
            string tier;
            int? capacity = null;

            switch (databaseSize)
            {
                case AzureDatabaseSize.S0:
                    name = tier = "Standard";
                    capacity = 10;
                    break;
                case AzureDatabaseSize.S3:
                    name = tier = "Standard";
                    capacity = 100;
                    break;
                case AzureDatabaseSize.S4:
                    name = tier = "Standard";
                    capacity = 200;
                    break;
                case AzureDatabaseSize.P15:
                    name = tier = "Premium";
                    capacity = 4000;
                    break;
                case AzureDatabaseSize.Basic:
                    name = tier = "Basic";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseSize), databaseSize, null);
            }

            dynamic skuObject = new System.Dynamic.ExpandoObject();
            skuObject.name = name;
            skuObject.tier = tier;
            skuObject.capacity = capacity;

            dynamic body = new System.Dynamic.ExpandoObject();
            body.sku = skuObject;

            return JsonConvert.SerializeObject(body);
        }
    }
}