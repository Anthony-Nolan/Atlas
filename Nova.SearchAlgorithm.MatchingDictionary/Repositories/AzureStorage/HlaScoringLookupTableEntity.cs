using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaScoringLookupTableEntity : HlaLookupTableEntity
    {
        public string HlaTypingCategoryAsString { get; set; }
        public HlaTypingCategory HlaTypingCategory => ParseStringToEnum<HlaTypingCategory>(HlaTypingCategoryAsString);

        public HlaScoringLookupTableEntity() { }

        public HlaScoringLookupTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
            : base(matchLocus, lookupName, typingMethod)
        {
        }
    }
}