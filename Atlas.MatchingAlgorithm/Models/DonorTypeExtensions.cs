using System;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Models
{
    internal static class DonorTypeExtensions
    {
        public static Atlas.Client.Models.Search.DonorType ToAtlasClientModel(this DonorType donorType)
                => donorType switch
                {
                    DonorType.Adult => Atlas.Client.Models.Search.DonorType.Adult,
                    DonorType.Cord => Atlas.Client.Models.Search.DonorType.Cord,
                    _ => throw new ArgumentOutOfRangeException(nameof(donorType), donorType, null)
                };

            public static DonorType Other(this DonorType type)
            {
                return type switch
                {
                    DonorType.Adult => DonorType.Cord,
                    DonorType.Cord => DonorType.Adult,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
    }
}
