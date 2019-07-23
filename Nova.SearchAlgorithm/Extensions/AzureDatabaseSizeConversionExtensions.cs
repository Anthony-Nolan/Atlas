using System;
using Nova.SearchAlgorithm.Models.AzureManagement;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class AzureDatabaseSizeConversionExtensions
    {
        public static AzureDatabaseSize ToAzureDatabaseSize(this string size)
        {
            return Enum.Parse<AzureDatabaseSize>(size);
        }
    }
}