using System;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo
{
    public class ScoringInfoBuilderFactory
    {
        public static IHlaScoringInfo GetDefaultScoringInfoFromBuilder(Type scoringInfoType)
        {
            if (scoringInfoType == typeof(SingleAlleleScoringInfo))
            {
                return new SingleAlleleScoringInfoBuilder().Build();
            }
            if (scoringInfoType == typeof(ConsolidatedMolecularScoringInfo))
            {
                return new ConsolidatedMolecularScoringInfoBuilder().Build();
            }
            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new MultipleAlleleScoringInfoBuilder().Build();
            }
            if (scoringInfoType == typeof(SerologyScoringInfo))
            {
                return new SerologyScoringInfoBuilder().Build();
            }

            throw new ArgumentException($"Unsupported type: {scoringInfoType}");
        }
    }
}