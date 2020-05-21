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
    public class HlaLookupTableEntity : TableEntity
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

        #region HlaScoringLookupTableEntity
        // This region should really be an entire separate sub-class, but AzureStorage doesn't really support that nicely :(
        // It's all related specifically to the Serialised data associated with Scoring LookupResults which is sub-typed depending on the
        // nature of the allele descriptor in question.

        [Obsolete("Deprecated in favour of " + nameof(SerialisedHlaInfoType))]//TODO: Delete this property once all the extant Storages have been re-generated.
        public string LookupNameCategoryAsString { get; set; }
        public string BackwardsCompatible_SerialisedHlaInfoType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SerialisedHlaInfoType))
                {
                    return SerialisedHlaInfoType;
                }

                var oldCategoryEnum = LookupNameCategoryAsString.ParseToEnum<HlaTypingCategory>();

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
                        return LookupNameCategoryAsString;
                }
            }
        }

        private static string SerialiseHlaInfo(object hlaInfo)
        {
            return JsonConvert.SerializeObject(hlaInfo);
        }

        public T GetHlaInfo<T>()
        {
            return JsonConvert.DeserializeObject<T>(SerialisedHlaInfo);
        }

        public IHlaScoringInfo DeserialiseTypedScoringInfo()
        {
            switch (BackwardsCompatible_SerialisedHlaInfoType)
            {
                case nameof(SerologyScoringInfo):
                    return GetHlaInfo<SerologyScoringInfo>();
                case nameof(SingleAlleleScoringInfo):
                    return GetHlaInfo<SingleAlleleScoringInfo>();
                case nameof(MultipleAlleleScoringInfo):
                    return GetHlaInfo<MultipleAlleleScoringInfo>();
                case nameof(ConsolidatedMolecularScoringInfo):
                    return GetHlaInfo<ConsolidatedMolecularScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
    }
}