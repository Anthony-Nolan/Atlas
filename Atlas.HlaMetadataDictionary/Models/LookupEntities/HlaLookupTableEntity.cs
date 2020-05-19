using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using EnumStringValues;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using static Atlas.Common.GeneticData.Hla.Models.HlaTypingCategory;

namespace Atlas.HlaMetadataDictionary.Models.LookupEntities
{
    internal class HlaLookupTableEntity : TableEntity
    {
        public string LocusAsString { get; set; }
        public Locus Locus => LocusAsString.ParseToEnum<Locus>();
        public string TypingMethodAsString { get; set; }
        public TypingMethod TypingMethod => TypingMethodAsString.ParseToEnum<TypingMethod>();
        public string LookupName { get; set; }
        public string SerialisedHlaInfoType { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(IHlaLookupResult lookupResult)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(lookupResult.Locus),
                HlaLookupTableKeyManager.GetEntityRowKey(lookupResult.LookupName, lookupResult.TypingMethod))
        {
            LocusAsString = lookupResult.Locus.ToString();
            TypingMethodAsString = lookupResult.TypingMethod.ToString();
            LookupName = lookupResult.LookupName;
            SerialisedHlaInfoType = lookupResult.HlaInfoToSerialise.GetType().Name; 
            SerialisedHlaInfo = SerialiseHlaInfo(lookupResult.HlaInfoToSerialise);
        }

        [Obsolete("Deprecated in favour of " + nameof(SerialisedHlaInfoType))]//TODO: ATLAS-282 Delete this property once all the extant Storages have been re-generated.
        public string LookupNameCategoryAsString { get; set; }

        private static string SerialiseHlaInfo(object hlaInfo)
        {
            return JsonConvert.SerializeObject(hlaInfo);
        }

        public T GetHlaInfo<T>()
        {
            return JsonConvert.DeserializeObject<T>(SerialisedHlaInfo);
        }
    }

    /// There are a bunch of extension methods on HlaLookupTableEntity trying to work around the fact that Azure Storage
    /// doesn't support sub-typing at all nicely. All the other extension methods are in separate sibling files.
    /// This one is here in this file because it is so tightly integrated with BackwardsCompatible_SerialisedHlaInfoType.
    /// TODO: ATLAS-282. Once that's gone, it can be moved into a separate sibling file like the other extension classes.
    internal static class HlaScoringLookupResultExtensions
    {
        public static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = entity.DeserialiseTypedScoringInfo();

            return new HlaScoringLookupResult(
                entity.Locus,
                entity.LookupName,
                scoringInfo,
                entity.TypingMethod);
        }

        private static IHlaScoringInfo DeserialiseTypedScoringInfo(this HlaLookupTableEntity entity)
        {
            switch (entity.BackwardsCompatibleScoringHlaInfoType())
            {
                case nameof(SerologyScoringInfo):
                    return entity.GetHlaInfo<SerologyScoringInfo>();
                case nameof(SingleAlleleScoringInfo):
                    return entity.GetHlaInfo<SingleAlleleScoringInfo>();
                case nameof(MultipleAlleleScoringInfo):
                    return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
                case nameof(ConsolidatedMolecularScoringInfo):
                    return entity.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string BackwardsCompatibleScoringHlaInfoType(this HlaLookupTableEntity entity) //TODO: ATLAS-282 Delete this entirely, just use SerialisedHlaInfoType directly.
        {
            if (!string.IsNullOrWhiteSpace(entity.SerialisedHlaInfoType))
            {
                return entity.SerialisedHlaInfoType;
            }

            var oldCategoryEnum = entity.LookupNameCategoryAsString.ParseToEnum<HlaTypingCategory>();

            switch (oldCategoryEnum)
            {
                case Serology:
                    return nameof(SerologyScoringInfo);
                case Allele:
                    return nameof(SingleAlleleScoringInfo);
                case NmdpCode:
                    return nameof(MultipleAlleleScoringInfo);
                case XxCode:
                    return nameof(ConsolidatedMolecularScoringInfo);
                default:
                    return entity.LookupNameCategoryAsString;
            }
        }

    }
}