using System;
using System.Collections.Generic;
using System.Linq;
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
        public string TypingMethodAsString { get; set; }
        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }
        public Locus Locus => LocusAsString.ParseToEnum<Locus>();
        public TypingMethod TypingMethod => TypingMethodAsString.ParseToEnum<TypingMethod>();

        public HlaLookupTableEntity() { }

        private HlaLookupTableEntity(IHlaLookupResult lookupResult, bool allowComplexSerialisation)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(lookupResult.Locus),
                HlaLookupTableKeyManager.GetEntityRowKey(lookupResult.LookupName, lookupResult.TypingMethod))
        {
            LocusAsString = lookupResult.Locus.ToString();
            TypingMethodAsString = lookupResult.TypingMethod.ToString();
            LookupName = lookupResult.LookupName;
            SerialisedHlaInfo = SerialiseHlaInfo(lookupResult.HlaInfoToSerialise, allowComplexSerialisation);
        }

        public HlaLookupTableEntity(IHlaLookupResult lookupResult)
            : this(lookupResult, false)
        { }

        #region HlaScoringLookupTableEntity
        // This region should really be an entire separate sub-class, but AzureStorage doesn't really support that nicely :(
        // It's all related specifically to the Serialised data associated with Scoring LookupResults which is sub-typed depending on the
        // nature of the allele descriptor in question.

        [Obsolete("Should only EVER be used INSIDE the Entity class. Wants to be private really, but needs to be public to be written to the AzureStorage")]
        public string HlaInfoTypeSerialised_AsString { get; set; }
        [Obsolete("Deprecated in favour of " + nameof(HlaInfoTypeSerialised_AsString))]//TODO: Delete this property once all the extant Storages have been re-generated.
        public string LookupNameCategoryAsString { get; set; }
        public HlaTypingCategory HlaInfoTypeSerialised
        {
            get
            {
                var stringToRead = string.IsNullOrWhiteSpace(HlaInfoTypeSerialised_AsString)
                    ? LookupNameCategoryAsString
                    : HlaInfoTypeSerialised_AsString;

                return stringToRead.ParseToEnum<HlaTypingCategory>();
            }
        }
        
        public HlaLookupTableEntity(IHlaLookupResult lookupResult, HlaTypingCategory hlaInfoTypeToBeSerialised)
            : this(lookupResult, true)
        {
            if (!new[]{Serology, Allele, NmdpCode, XxCode}.Contains(hlaInfoTypeToBeSerialised))
            {
                throw new InvalidOperationException($"This deserialisation process currently only support certain values of {nameof(HlaTypingCategory)} currently, of which '{hlaInfoTypeToBeSerialised}' is not one.");
            }
            HlaInfoTypeSerialised_AsString = hlaInfoTypeToBeSerialised.GetStringValue();
        }

        private static string SerialiseHlaInfo(object hlaInfo, bool allowComplexSerialisation)
        {
            if (hlaInfo is string || hlaInfo is IEnumerable<string>)
            {
                return JsonConvert.SerializeObject(hlaInfo);
            }

            if (hlaInfo is IHlaScoringInfo)
            {
                if (allowComplexSerialisation)
                {
                    return JsonConvert.SerializeObject(hlaInfo);
                }
                throw new InvalidOperationException("If you wish to create a TableEntity that serialises more complex Scoring data, then you must use the constructor that declares the nature of that Scoring data.");
            }

            throw new InvalidOperationException("This is an entirely new and currently unsupported usage of a TableEntity.");
        }

        public T GetHlaInfo<T>()
        {
            return JsonConvert.DeserializeObject<T>(SerialisedHlaInfo);
        }

        public IHlaScoringInfo DeserialiseTypedScoringInfo()
        {
            switch (HlaInfoTypeSerialised)
            {
                case Serology:
                    return GetHlaInfo<SerologyScoringInfo>();
                case Allele:
                    return GetHlaInfo<SingleAlleleScoringInfo>();
                case NmdpCode:
                    return GetHlaInfo<MultipleAlleleScoringInfo>();
                case XxCode:
                    return GetHlaInfo<ConsolidatedMolecularScoringInfo>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
    }
}