using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;

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

        public InputDonorBuilder WithHlaAtLocus(Locus locus, TypePosition position, string hla)
        {
            donor.HlaNames.SetAtPosition(locus, position, hla);
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