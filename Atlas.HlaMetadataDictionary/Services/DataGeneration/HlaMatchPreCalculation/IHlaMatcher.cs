using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    internal interface IHlaMatcher
    {
        IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo);
    }
}
