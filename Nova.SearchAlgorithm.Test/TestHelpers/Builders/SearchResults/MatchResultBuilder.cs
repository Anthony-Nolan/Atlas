using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.SearchResults;

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
            matchResult.Donor.HlaNames.SetAtLocus(locus, hla);
            return this;
        }

        public MatchResult Build()
        {
            matchResult.MarkMatchingDataFullyPopulated();
            return matchResult;
        }
    }
}