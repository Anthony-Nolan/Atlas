using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults
{
    internal class MatchResultBuilder
    {
        private readonly MatchResult matchResult;

        public MatchResultBuilder()
        {
            matchResult = new MatchResult(IncrementingIdGenerator.NextIntId())
            {
                DonorInfo = new DonorInfoWithExpandedHla
                {
                    HlaNames = new PhenotypeInfo<string>("donor-hla")
                }
            };
        }

        public MatchResultBuilder WithMatchCountAtLocus(Locus locus, int matchCount)
        {
            var locusMatchDetails = matchResult.MatchDetails.GetLocus(locus) ?? new LocusMatchDetails();

            locusMatchDetails.PositionPairs = matchCount switch
            {
                0 => new HashSet<(LocusPosition, LocusPosition)>(),
                1 => new HashSet<(LocusPosition, LocusPosition)> {(LocusPosition.One, LocusPosition.One)},
                2 => new HashSet<(LocusPosition, LocusPosition)> {(LocusPosition.One, LocusPosition.One), (LocusPosition.Two, LocusPosition.Two)},
                _ => throw new ArgumentOutOfRangeException(nameof(matchCount))
            };

            matchResult.SetMatchDetailsForLocus(locus, locusMatchDetails);
            return this;
        }

        public MatchResultBuilder WithHlaAtLocus(Locus locus, string hla)
        {
            matchResult.DonorInfo.HlaNames = matchResult.DonorInfo.HlaNames.SetLocus(locus, hla);
            return this;
        }

        public MatchResult Build()
        {
            matchResult.MarkMatchingDataFullyPopulated();
            return matchResult;
        }
    }
}