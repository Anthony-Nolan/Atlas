using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    internal interface IHlaMatcher
    {
        IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo);
    }
}
