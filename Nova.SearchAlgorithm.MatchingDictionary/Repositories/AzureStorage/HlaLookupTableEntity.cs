using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaLookupTableEntity : TableEntity
    {
        public string MatchLocusAsString { get; set; }
        public string TypingMethodAsString { get; set; }
        public string LookupCategoryAsString { get; set; }
        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public MatchLocus MatchLocus => ParseStringToEnum<MatchLocus>(MatchLocusAsString);
        public TypingMethod TypingMethod => ParseStringToEnum<TypingMethod>(TypingMethodAsString);
        public LookupCategory LookupCategory => ParseStringToEnum<LookupCategory>(LookupCategoryAsString);

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(
            IHlaLookupResult lookupResult,
            object hlaInfo)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(lookupResult.MatchLocus), 
                   HlaLookupTableKeyManager.GetEntityRowKey(lookupResult.LookupName, lookupResult.TypingMethod))
        {
            MatchLocusAsString = lookupResult.MatchLocus.ToString();
            TypingMethodAsString = lookupResult.TypingMethod.ToString();
            LookupName = lookupResult.LookupName;
            SerialisedHlaInfo = SerialiseHlaInfo(hlaInfo);
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