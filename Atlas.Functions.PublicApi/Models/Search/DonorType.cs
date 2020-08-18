using System;

namespace Atlas.Functions.PublicApi.Models.Search
{
    public enum DonorType
    {
        Adult,
        Cord
    }

    internal static class DonorTypeMappings
    {
        // TODO: ALAS-290: clean up DonorType enums 
        public static MatchingAlgorithm.Client.Models.Donors.DonorType ToMatchingAlgorithmDonorType(this DonorType donorType)
        {
            return donorType switch
            {
                DonorType.Adult => MatchingAlgorithm.Client.Models.Donors.DonorType.Adult,
                DonorType.Cord => MatchingAlgorithm.Client.Models.Donors.DonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(donorType))
            };
        }
    }
}