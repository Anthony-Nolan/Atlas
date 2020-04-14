using System;
using Atlas.MatchingAlgorithm.Models.AzureManagement;

namespace Atlas.MatchingAlgorithm.Extensions
{
    public static class AzureDatabaseSizeConversionExtensions
    {
        public static AzureDatabaseSize ToAzureDatabaseSize(this string size)
        {
            return Enum.Parse<AzureDatabaseSize>(size);
        }
    }
}