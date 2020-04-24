using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Client.Models;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders
{
    public class MetaDonorBuilder
    {
        private readonly MetaDonor metaDonor;

        public MetaDonorBuilder()
        {
            metaDonor = new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build()
            };
        }

        public MetaDonorBuilder WithDonorType(DonorType donorType)
        {
            metaDonor.DonorType = donorType;
            return this;
        }

        public MetaDonorBuilder WithGenotypeCriteria(GenotypeCriteria criteria)
        {
            metaDonor.GenotypeCriteria = criteria;
            return this;
        }

        public MetaDonor Build()
        {
            return metaDonor;
        }
    }
}