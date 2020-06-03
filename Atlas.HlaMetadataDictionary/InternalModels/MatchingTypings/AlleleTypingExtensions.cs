using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using EnumStringValues;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal static class AlleleTypingExtensions
    {
        public static AlleleTypingStatus ToAlleleTypingStatus(this AlleleStatus alleleStatus)
        {
            if (alleleStatus == null)
            {
                return AlleleTypingStatus.GetDefaultStatus();
            }

            if (!alleleStatus.SequenceStatus.TryParseStringValueToEnum<SequenceStatus>(out var sequenceStatus))
            {
                throw new HlaMetadataDictionaryException(alleleStatus, $"Sequence status {alleleStatus.SequenceStatus} not recognised.");
            }

            if (!alleleStatus.DnaCategory.TryParseStringValueToEnum<DnaCategory>(out var dnaCategory))
            {
                    throw new HlaMetadataDictionaryException(alleleStatus, $"DNA category {alleleStatus.DnaCategory} not recognised.");
            }

            return new AlleleTypingStatus(sequenceStatus, dnaCategory);
        }
    }
}
