using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class MetaDonorBuilder
    {
        private readonly MetaDonor metaDonor;

        public MetaDonorBuilder()
        {
            metaDonor = new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build()
            };
        }

        public MetaDonorBuilder WithDonorType(DonorType donorType)
        {
            metaDonor.DonorType = donorType;
            return this;
        }

        public MetaDonorBuilder AtRegistry(RegistryCode registryCode)
        {
            metaDonor.Registry = registryCode;
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