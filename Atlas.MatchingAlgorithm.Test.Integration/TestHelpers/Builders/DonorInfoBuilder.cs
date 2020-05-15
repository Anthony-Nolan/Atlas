using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class DonorInfoBuilder
    {
        private readonly DonorInfo donorInfo;

        public DonorInfoBuilder(int donorId)
        {
            donorInfo = new DonorInfo
            {
                DonorType = DonorType.Adult,
                DonorId = donorId,
                // Default hla chosen to be valid hla
                HlaNames = new PhenotypeInfo<string>
                {
                    A =
                    {
                        Position1 = "*01:01",
                        Position2 = "*01:01",
                    },
                    B =
                    {
                        Position1 = "*18:01:01",
                        Position2 = "*18:01:01",
                    },
                    Drb1 =
                    {
                        Position1 = "*04:01",
                        Position2 = "*04:01",
                    }
                }
            };
        }

        public DonorInfoBuilder WithHlaAtLocus(Locus locus, TypePosition position, string hla)
        {
            donorInfo.HlaNames.SetAtPosition(locus, position, hla);
            return this;
        }

        public DonorInfoBuilder WithDonorType(DonorType donorType)
        {
            donorInfo.DonorType = donorType;
            return this;
        }

        public DonorInfo Build()
        {
            return donorInfo;
        }
    }
}