using System;
using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    public enum DonorType
    {
        [EnumMember(Value = "D")] Adult,
        [EnumMember(Value = "C")] Cord,
    }

    public static class DonorTypeExtensions
    {
        public static Data.Models.DonorType ToDatabaseType(this DonorType fileDonorType)
        {
            return fileDonorType switch
            {
                DonorType.Adult => Data.Models.DonorType.Adult,
                DonorType.Cord => Data.Models.DonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(fileDonorType), fileDonorType, null)
            };
        }
        public static MatchingAlgorithm.Client.Models.Donors.DonorType ToMatchingAlgorithmType(this DonorType fileDonorType)
        {
            return fileDonorType switch
            {
                DonorType.Adult => MatchingAlgorithm.Client.Models.Donors.DonorType.Adult,
                DonorType.Cord => MatchingAlgorithm.Client.Models.Donors.DonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(fileDonorType), fileDonorType, null)
            };
        }
    }
}