using System;
using Atlas.DonorImport.Data.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Models.Mapping
{
    internal static class DatabaseDonorTypeMaps
    {
        public static DonorType ToMatchingAlgorithmType(this DatabaseDonorType databaseDonorType)
        {
            return databaseDonorType switch
            {
                DatabaseDonorType.Adult => DonorType.Adult,
                DatabaseDonorType.Cord => DonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(databaseDonorType), databaseDonorType, null)
            };
        }
    }
}