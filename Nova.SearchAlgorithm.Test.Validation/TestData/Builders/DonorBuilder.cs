using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class DonorBuilder
    {
        private readonly Donor donor;
        private readonly Genotype genotype;

        public DonorBuilder(Genotype genotype)
        {
            this.genotype = genotype;
            donor = new Donor {DonorId = DonorIdGenerator.NextId()};
        }

        public DonorBuilder OfType(DonorType donorType)
        {
            donor.DonorType = donorType;
            return this;
        }
        
        public DonorBuilder AtRegistry(RegistryCode registryCode)
        {
            donor.RegistryCode = registryCode;
            return this;
        }

        public DonorBuilder WithTypingCategories(PhenotypeInfo<HlaTypingResolution> typingCategorySet)
        {
            donor.A_1 = genotype.Hla.A_1.GetHlaForCategory(typingCategorySet.A_1);
            donor.A_2 = genotype.Hla.A_2.GetHlaForCategory(typingCategorySet.A_2);
            donor.B_1 = genotype.Hla.B_1.GetHlaForCategory(typingCategorySet.B_1);
            donor.B_2 = genotype.Hla.B_2.GetHlaForCategory(typingCategorySet.B_2);
            donor.DRB1_1 = genotype.Hla.DRB1_1.GetHlaForCategory(typingCategorySet.DRB1_1);
            donor.DRB1_2 = genotype.Hla.DRB1_2.GetHlaForCategory(typingCategorySet.DRB1_2);
            donor.C_1 = genotype.Hla.C_1.GetHlaForCategory(typingCategorySet.C_1);
            donor.C_2 = genotype.Hla.C_2.GetHlaForCategory(typingCategorySet.C_2);
            donor.DQB1_1 = genotype.Hla.DQB1_1.GetHlaForCategory(typingCategorySet.DQB1_1);
            donor.DQB1_2 = genotype.Hla.DQB1_2.GetHlaForCategory(typingCategorySet.DQB1_2);
            return this;
        }

        public Donor Build()
        {
            return donor;
        }
    }
}