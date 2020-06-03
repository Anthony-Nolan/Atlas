using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    internal interface IHlaMatcher
    {
        IEnumerable<IMatchedHla> PreCalculateMatchedHla(HlaInfoForMatching hlaInfo);
    }
}
