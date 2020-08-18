using System;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Client.Models.Donors
{
    public enum DonorType
    {
        // Do not renumber, these values are stored in the database as integers.
        [StringValue("adult"), StringValue("a")]
        Adult = 1, // AKA: MUD

        [StringValue("cord"), StringValue("c")]
        Cord = 2 // AKA: CBU
    }

    public static class Extension
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