using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    internal interface IHlaMatcher
    {
        IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo);
    }
}
