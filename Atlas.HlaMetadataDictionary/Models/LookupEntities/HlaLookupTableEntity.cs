using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Services.AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Models.LookupEntities
{
    public class HlaLookupTableEntity : TableEntity
    {
        public string LocusAsString { get; set; }
        public string TypingMethodAsString { get; set; }
        public string LookupNameCategoryAsString { get; set; }
        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public Locus Locus => ParseStringToEnum<Locus>(LocusAsString);
        public TypingMethod TypingMethod => ParseStringToEnum<TypingMethod>(TypingMethodAsString);
        public LookupNameCategory LookupNameCategory => ParseStringToEnum<LookupNameCategory>(LookupNameCategoryAsString);

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(IHlaLookupResult lookupResult)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(lookupResult.Locus), 
                   HlaLookupTableKeyManager.GetEntityRowKey(lookupResult.LookupName, lookupResult.TypingMethod))
        {
            LocusAsString = lookupResult.Locus.ToString();
            TypingMethodAsString = lookupResult.TypingMethod.ToString();
            LookupName = lookupResult.LookupName;
            SerialisedHlaInfo = SerialiseHlaInfo(lookupResult.HlaInfoToSerialise);
        }

        public T GetHlaInfo<T>()
        {
            return JsonConvert.DeserializeObject<T>(SerialisedHlaInfo);
        }

        private static string SerialiseHlaInfo(object hlaInfo)
        {
            return JsonConvert.SerializeObject(hlaInfo);
        }

        private static TEnum ParseStringToEnum<TEnum>(string str)
        {
            return string.IsNullOrEmpty(str)
                ? throw new ArgumentException($"Cannot convert empty string to {typeof(TEnum).Name}.")
                : (TEnum)Enum.Parse(typeof(TEnum), str);
        }
    }
}