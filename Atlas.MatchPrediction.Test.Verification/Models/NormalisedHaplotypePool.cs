using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Models.FileSchema;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    public class NormalisedHaplotypePool
    {
        public int Id { get; }
        public string HlaNomenclatureVersion { get; }
        public ImportTypingCategory TypingCategory { get; }
        public IReadOnlyCollection<NormalisedPoolMember> PoolMembers { get; }
        public int TotalCopyNumber { get; }

        public NormalisedHaplotypePool(
            int poolId,
            string hlaNomenclatureVersion,
            ImportTypingCategory typingCategory,
            IReadOnlyCollection<NormalisedPoolMember> poolMembers)
        {
            Id = poolId;
            HlaNomenclatureVersion = hlaNomenclatureVersion;
            TypingCategory = typingCategory;
            PoolMembers = poolMembers;
            TotalCopyNumber = poolMembers.Sum(h => h.CopyNumber);
        }

        public FrequencyRecord GetHaplotypeFrequencyByPoolIndex(int poolIndex)
        {
            if (poolIndex < 0)
            {
                throw new ArgumentException($"{nameof(poolIndex)} value cannot be negative.");
            }

            if (poolIndex >= TotalCopyNumber)
            {
                throw new ArgumentException($"{nameof(poolIndex)} value cannot be larger than {TotalCopyNumber-1}");
            }

            return PoolMembers
                .Single(p => p.PoolIndexLowerBoundary <= poolIndex && poolIndex <= p.PoolIndexUpperBoundary)
                .HaplotypeFrequency;
        }
    }

    public class NormalisedPoolMember
    {
        public FrequencyRecord HaplotypeFrequency { get; set; }
        public int CopyNumber { get; set; }
        public int PoolIndexLowerBoundary { get; set; }
        public int PoolIndexUpperBoundary => PoolIndexLowerBoundary + CopyNumber - 1;
    }
}
