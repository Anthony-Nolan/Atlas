using Atlas.MatchingAlgorithm.MatchingDictionary.Exceptions;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using System;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    internal static class AlleleTypingExtensions
    {
        public static AlleleTypingStatus ToAlleleTypingStatus(this AlleleStatus alleleStatus)
        {
            if (alleleStatus == null)
            {
                return AlleleTypingStatus.GetDefaultStatus();
            }

            var hlaInfo = new HlaInfo(alleleStatus.TypingLocus, alleleStatus.Name);

            if (!Enum.TryParse(alleleStatus.SequenceStatus, true, out SequenceStatus sequenceStatus))
            {
                throw new MatchingDictionaryException(hlaInfo, $"Sequence status {alleleStatus.SequenceStatus} not recognised.");
            }

            if (!Enum.TryParse(alleleStatus.DnaCategory, true, out DnaCategory dnaCategory))
            {
                throw new MatchingDictionaryException(hlaInfo, $"DNA category {alleleStatus.DnaCategory} not recognised.");
            }

            return new AlleleTypingStatus(sequenceStatus, dnaCategory);
        }
    }
}
