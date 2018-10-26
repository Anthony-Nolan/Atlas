using System;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    /// <summary>
    /// Builds a donor froma given meta-donor's genotype
    /// </summary>
    public class DonorBuilder
    {
        private readonly Donor donor;
        private readonly Genotype genotype;

        private PhenotypeInfo<HlaTypingResolution> typingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        private PhenotypeInfo<bool> shouldMatchGenotype = new PhenotypeInfo<bool>(true);
        
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

        public DonorBuilder WithShouldMatchGenotype(PhenotypeInfo<bool> shouldMatchGenotypeData)
        {
            shouldMatchGenotype = shouldMatchGenotypeData;
            return this;
        }

        public Donor Build()
        {
            donor.A_1 = GetHla(Locus.A, TypePosition.One);
            donor.A_2 = GetHla(Locus.A, TypePosition.Two);
            donor.B_1 = GetHla(Locus.B, TypePosition.One);
            donor.B_2 = GetHla(Locus.B, TypePosition.Two);
            donor.C_1 = GetHla(Locus.C, TypePosition.One);
            donor.C_2 = GetHla(Locus.C, TypePosition.Two);
            donor.DPB1_1 = GetHla(Locus.Dpb1, TypePosition.One);
            donor.DPB1_2 = GetHla(Locus.Dpb1, TypePosition.Two);
            donor.DQB1_1 = GetHla(Locus.Dqb1, TypePosition.One);
            donor.DQB1_2 = GetHla(Locus.Dqb1, TypePosition.Two);
            donor.DRB1_1 = GetHla(Locus.Drb1, TypePosition.One);
            donor.DRB1_2 = GetHla(Locus.Drb1, TypePosition.Two);
            return donor;
        }

        private string GetHla(Locus locus, TypePosition position)
        {
            var shouldMatchGenotypeAtPosition = shouldMatchGenotype.DataAtPosition(locus, position);
            var resolution = typingResolutions.DataAtPosition(locus, position);

            return shouldMatchGenotypeAtPosition ? GetMatchingHla(locus, position, resolution) : GetNonMatchingHla(locus, resolution);
        }

        private string GetMatchingHla(Locus locus, TypePosition position, HlaTypingResolution resolution)
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