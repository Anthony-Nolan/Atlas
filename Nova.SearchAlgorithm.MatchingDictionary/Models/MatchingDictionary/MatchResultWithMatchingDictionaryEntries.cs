using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models
{
    public class MatchResultWithMatchingDictionaryEntries
    {
        public MatchResult MatchResult { get; set; }
        public PhenotypeInfo<IEnumerable<MatchingDictionaryEntry>> MatchingDictionaryEntries { get; set; }
    }
}