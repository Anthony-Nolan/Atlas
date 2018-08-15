using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders.SearchResults
{
    public class MatchResultBuilder
    {
        private readonly MatchResult matchResult;

        public MatchResultBuilder()
        {
            matchResult = new MatchResult
            {
                Donor = new DonorResult
                {
                    HlaNames = new PhenotypeInfo<string>
                    {
                        A_1 = "donor-hla",
                        A_2 = "donor-hla",
                        B_1 = "donor-hla",
                        B_2 = "donor-hla",
                        C_1 = "donor-hla",
                        C_2 = "donor-hla",
                        DQB1_1 = "donor-hla",
                        DQB1_2 = "donor-hla",
                        DRB1_1 = "donor-hla",
                        DRB1_2 = "donor-hla",
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
            matchResult.Donor.HlaNames.SetAtLocus(locus, TypePositions.Both, hla);
            return this;
        }

        public MatchResult Build()
        {
            matchResult.MarkMatchingDataFullyPopulated();
            return matchResult;
        }
    }
}