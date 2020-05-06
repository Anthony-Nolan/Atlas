using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    internal interface IHlaMatcher
    {
        IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo);
    }
}
