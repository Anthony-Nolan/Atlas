using System;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class DonorBuilder
    {
        private readonly Donor donor;
        private readonly Genotype genotype;

        private PhenotypeInfo<HlaTypingResolution> typingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        private readonly PhenotypeInfo<bool> shouldMatchGenotype = new PhenotypeInfo<bool>(true);
        
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

        public DonorBuilder WithTypingCategories(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            typingResolutions = resolutions;
            return this;
        }

        public Donor Build()
        {
            donor.A_1 = GetHla(Locus.A, TypePositions.One);
            donor.A_2 = GetHla(Locus.A, TypePositions.Two);
            donor.B_1 = GetHla(Locus.B, TypePositions.One);
            donor.B_2 = GetHla(Locus.B, TypePositions.Two);
            donor.C_1 = GetHla(Locus.C, TypePositions.One);
            donor.C_2 = GetHla(Locus.C, TypePositions.Two);
            donor.DPB1_1 = GetHla(Locus.Dpb1, TypePositions.One);
            donor.DPB1_2 = GetHla(Locus.Dpb1, TypePositions.Two);
            donor.DQB1_1 = GetHla(Locus.Dqb1, TypePositions.One);
            donor.DQB1_2 = GetHla(Locus.Dqb1, TypePositions.Two);
            donor.DRB1_1 = GetHla(Locus.Drb1, TypePositions.One);
            donor.DRB1_2 = GetHla(Locus.Drb1, TypePositions.Two);
            return donor;
        }

        private string GetHla(Locus locus, TypePositions position)
        {
            var shouldMatchGenotypeAtPosition = shouldMatchGenotype.DataAtPosition(locus, position);
            var resolution = typingResolutions.DataAtPosition(locus, position);

            return shouldMatchGenotypeAtPosition ? GetMatchingHla(locus, position, resolution) : GetNonMatchingHla(locus, resolution);
        }

        private string GetMatchingHla(Locus locus, TypePositions position, HlaTypingResolution resolution)
        {
            return genotype.Hla.DataAtPosition(locus, position).GetHlaForResolution(resolution);
        }

        private static string GetNonMatchingHla(Locus locus, HlaTypingResolution resolution)
        {
            var tgsAllele = TgsAllele.FromTestDataAllele(NonMatchingAlleles.NonMatchingDonorAlleles.DataAtLocus(locus));
            return tgsAllele.GetHlaForResolution(resolution);
        }
    }
}