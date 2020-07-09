using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class DonorInfoBuilder
    {
        private readonly DonorInfo donorInfo;

        /// <summary>
        /// Builds a Donor, using the generated NextId, unless specifically overridden.
        /// The override should only be used if you explicitly want to re-use an existing donor's Id.
        /// </summary>
        /// <param name="donorId"></param>
        public DonorInfoBuilder(int? donorId = null)
        {
            donorInfo = new DonorInfo
            {
                DonorType = DonorType.Adult,
                DonorId = donorId ?? DonorIdGenerator.NextId(),
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

        public DonorInfoBuilder WithHlaAtLocus(Locus locus, LocusPosition position, string hla)
        {
            donorInfo.HlaNames.SetPosition(locus, position, hla);
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