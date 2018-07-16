using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System;
using Nova.HLAService.Client.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaLookupTableEntity : TableEntity
    {
        public string MatchLocusAsString { get; set; }
        public string TypingMethodAsString { get; set; }
        public string HlaTypingCategoryAsString { get; set; }
        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public MatchLocus MatchLocus => ParseStringToEnum<MatchLocus>(MatchLocusAsString);
        public TypingMethod TypingMethod => ParseStringToEnum<TypingMethod>(TypingMethodAsString);
        public HlaTypingCategory HlaTypingCategory => ParseStringToEnum<HlaTypingCategory>(HlaTypingCategoryAsString);

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
            : base(HlaLookupTableKeyManager.GetEntityPartitionKey(matchLocus), 
                   HlaLookupTableKeyManager.GetEntityRowKey(lookupName, typingMethod))
        {
            MatchLocusAsString = matchLocus.ToString();
            TypingMethodAsString = typingMethod.ToString();
            LookupName = lookupName;
        }

        private static TEnum ParseStringToEnum<TEnum>(string str)
        {
            return string.IsNullOrEmpty(str)
                ? throw new ArgumentException($"Cannot convert empty string to {typeof(TEnum).Name}.")
                : (TEnum)Enum.Parse(typeof(TEnum), str);
        }
    }
}