﻿using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults
{
    public class MatchResultBuilder
    {
        private readonly MatchResult matchResult;

        public MatchResultBuilder()
        {
            matchResult = new MatchResult
            {
                DonorInfo = new DonorInfoWithExpandedHla
                {
                    HlaNames = new PhenotypeInfo<string>
                    {
                        A = { Position1 = "donor-hla", Position2 = "donor-hla"},
                        B = { Position1 = "donor-hla", Position2 = "donor-hla"},
                        C = { Position1 = "donor-hla", Position2 = "donor-hla"},
                        Dpb1 = { Position1 = "donor-hla", Position2 = "donor-hla" },
                        Dqb1 = { Position1 = "donor-hla", Position2 = "donor-hla"},
                        Drb1 = { Position1 = "donor-hla", Position2 = "donor-hla"},
                    }
                }
            };
        }

        public MatchResultBuilder WithMatchCountAtLocus(Locus locus, int matchCount)
        {
            matchResult.SetMatchDetailsForLocus(locus, new LocusMatchDetails
            {
                MatchCount = matchCount
            });
            return this;
        }

        public MatchResultBuilder WithHlaAtLocus(Locus locus, string hla)
        {
            matchResult.DonorInfo.HlaNames.SetLocus(locus, hla);
            return this;
        }

        public MatchResult Build()
        {
            matchResult.MarkMatchingDataFullyPopulated();
            return matchResult;
        }
    }
}