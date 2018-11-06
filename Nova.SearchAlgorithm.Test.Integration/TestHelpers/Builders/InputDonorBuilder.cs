using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class InputDonorBuilder
    {
        private readonly InputDonor donor;
        
        public InputDonorBuilder(int donorId)
        {
            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = donorId,
                // Default hla chosen to be valid hla
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = "*01:01",
                    A_2 = "*01:01",
                    B_1 = "*18:01:01",
                    B_2 = "*18:01:01",
                    Drb1_1 = "*04:01",
                    Drb1_2 = "*04:01",
                }
            };
        }

        public InputDonorBuilder WithHlaAtLocus(Locus locus, TypePosition position, string hla)
        {
            ((PhenotypeInfo<string>) donor.HlaNames).SetAtPosition(locus, position, hla);
            return this;
        }

        public InputDonorBuilder WithRegistryCode(RegistryCode registryCode)
        {
            donor.RegistryCode = registryCode;
            return this;
        }
        
        public InputDonorBuilder WithDonorType(DonorType donorType)
        {
            donor.DonorType = donorType;
            return this;
        }
        
        public InputDonor Build()
        {
            return donor;
        }
    }
}