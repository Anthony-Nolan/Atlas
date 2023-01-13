using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

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
        internal DonorInfoBuilder(int? donorId = null)
        {
            donorInfo = new DonorInfo
            {
                DonorType = DonorType.Adult,
                DonorId = donorId ?? DonorIdGenerator.NextId(),
                // Default hla chosen to be valid hla
                HlaNames = new PhenotypeInfo<string>
                (
                    valueA: new LocusInfo<string>("*01:01", "*01:01"),
                    valueB: new LocusInfo<string>("*18:01:01", "*18:01:01"),
                    valueDrb1: new LocusInfo<string>("*04:01", "*04:01")
                )
            };
        }

        internal DonorInfoBuilder(Donor existingDonor)
        {
            donorInfo = existingDonor.ToDonorInfo();
        }

        internal DonorInfoBuilder WithHlaAtLocus(Locus locus, LocusPosition position, string hla)
        {
            donorInfo.HlaNames = donorInfo.HlaNames.SetPosition(locus, position, hla);
            return this;
        }

        internal DonorInfoBuilder WithDonorType(DonorType donorType)
        {
            donorInfo.DonorType = donorType;
            return this;
        }

        internal DonorInfo Build() => donorInfo;
    }
}