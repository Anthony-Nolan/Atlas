using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Integration.FileBackedMatchingDictionary
{
    public class RawMatchingHla
    {
        public string MatchLocus { get; set; }
        public string LookupName { get; set; }
        public string TypingMethod { get; set; }
        public List<string> MatchingPGroups { get; set; }
        public List<string> MatchingGGroups { get; set; }
    }
}