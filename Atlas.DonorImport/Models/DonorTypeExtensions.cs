using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.FileSchema.Models;
using System;

namespace Atlas.DonorImport.Models
{
    internal static class DonorTypeExtensions
    {
        public static DatabaseDonorType ToDatabaseType(this ImportDonorType fileDonorType)
        {
            return fileDonorType switch
            {
                ImportDonorType.Adult => DatabaseDonorType.Adult,
                ImportDonorType.Cord => DatabaseDonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(fileDonorType), fileDonorType, null)
            };
        }

        public static MatchingAlgorithm.Client.Models.Donors.DonorType ToMatchingAlgorithmType(this ImportDonorType fileDonorType)
        {
            return fileDonorType switch
            {
                ImportDonorType.Adult => MatchingAlgorithm.Client.Models.Donors.DonorType.Adult,
                ImportDonorType.Cord => MatchingAlgorithm.Client.Models.Donors.DonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(fileDonorType), fileDonorType, null)
            };
        }
    }
}
