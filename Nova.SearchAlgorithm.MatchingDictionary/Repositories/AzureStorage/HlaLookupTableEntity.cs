using System;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaLookupTableEntity : TableEntity
    {
        public string MatchLocusAsString { get; set; }
        public string TypingMethodAsString { get; set; }
        
        // TODO: nova-1445: set enum as string to store in table
        public HlaTypingCategory HlaTypingCategory { get; set; }

        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public MatchLocus MatchLocus => ParseStringToEnum<MatchLocus>(MatchLocusAsString);
        public TypingMethod TypingMethod => ParseStringToEnum<TypingMethod>(TypingMethodAsString);

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
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }
    }
}