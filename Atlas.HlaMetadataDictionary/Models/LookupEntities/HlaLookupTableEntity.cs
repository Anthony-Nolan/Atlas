using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using EnumStringValues;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using static Atlas.Common.Utils.Extensions.TypeExtensions;

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

        public HlaLookupTableEntity(ISerialisableHlaMetadata lookupResult)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(lookupResult.Locus),
                HlaLookupTableKeyManager.GetEntityRowKey(lookupResult.LookupName, lookupResult.TypingMethod))
        {
            LocusAsString = lookupResult.Locus.ToString();
            TypingMethodAsString = lookupResult.TypingMethod.ToString();
            LookupName = lookupResult.LookupName;
            SerialisedHlaInfoType = lookupResult.HlaInfoToSerialise.GetType().GetNeatCSharpName(); //See note below, in GetHlaInfo<T>()
            SerialisedHlaInfo = SerialiseHlaInfo(lookupResult.HlaInfoToSerialise);
        }

        private static string SerialiseHlaInfo(object hlaInfo)
        {
            return JsonConvert.SerializeObject(hlaInfo);
        }

        public T GetHlaInfo<T>()
        {
            // Alas "nameof(T)", which would be compile-time constant, and thus compatible with a switch, doesn't give values that always match with typeof(T).Name
            // So we have to calculate these ourselves. `.Name` doesn't qualify Generic Types, and `.FullName` is incredibly verbose for Generic Types, hence using this helper.
            var typeName = typeof(T).GetNeatCSharpName();
            if (typeName != SerialisedHlaInfoType)
            {
                throw new InvalidOperationException($"Expected to find '{typeName}' data to be deserialised. But actually the data is labeled as '{SerialisedHlaInfoType}'. Unable to proceed.");
            }
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

        // Alas "nameof(T)", which would be compile-time constant, and thus compatible with a switch, doesn't give values that always match with typeof(T).Name
        // So we have to calculate these ourselves.
        private static readonly string SerologyInfoType = GetNeatCSharpName<SerologyScoringInfo>();
        private static readonly string SingleAlleleInfoType = GetNeatCSharpName<SingleAlleleScoringInfo>();
        private static readonly string MultipleAlleleInfoType = GetNeatCSharpName<MultipleAlleleScoringInfo>();
        private static readonly string ConsolidatedMolecularInfoType = GetNeatCSharpName<ConsolidatedMolecularScoringInfo>();

        private static IHlaScoringInfo DeserialiseTypedScoringInfo(this HlaLookupTableEntity entity)
        {
            var infoTypeString = entity.SerialisedHlaInfoType;
            if (infoTypeString == SerologyInfoType)
            {
                return entity.GetHlaInfo<SerologyScoringInfo>();
            }
            if (infoTypeString == SingleAlleleInfoType)
            {
                return entity.GetHlaInfo<SingleAlleleScoringInfo>();
            }
            if (infoTypeString == MultipleAlleleInfoType)
            {
                return entity.GetHlaInfo<MultipleAlleleScoringInfo>();
            }
            if (infoTypeString == ConsolidatedMolecularInfoType)
            {
                return entity.GetHlaInfo<ConsolidatedMolecularScoringInfo>();
            }
            throw new ArgumentOutOfRangeException();
        }
    }
}