using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders
{
    /// <summary>
    /// Builds a donor from a given meta-donor's genotype
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
            donor.A_1 = GetHla(Locus.A, LocusPosition.Position1);
            donor.A_2 = GetHla(Locus.A, LocusPosition.Position2);
            donor.B_1 = GetHla(Locus.B, LocusPosition.Position1);
            donor.B_2 = GetHla(Locus.B, LocusPosition.Position2);
            donor.C_1 = GetHla(Locus.C, LocusPosition.Position1);
            donor.C_2 = GetHla(Locus.C, LocusPosition.Position2);
            donor.DPB1_1 = GetHla(Locus.Dpb1, LocusPosition.Position1);
            donor.DPB1_2 = GetHla(Locus.Dpb1, LocusPosition.Position2);
            donor.DQB1_1 = GetHla(Locus.Dqb1, LocusPosition.Position1);
            donor.DQB1_2 = GetHla(Locus.Dqb1, LocusPosition.Position2);
            donor.DRB1_1 = GetHla(Locus.Drb1, LocusPosition.Position1);
            donor.DRB1_2 = GetHla(Locus.Drb1, LocusPosition.Position2);
            return donor;
        }

        private string GetHla(Locus locus, LocusPosition position)
        {
            var shouldMatchGenotypeAtPosition = shouldMatchGenotype.GetPosition(locus, position);
            var resolution = typingResolutions.GetPosition(locus, position);

            return shouldMatchGenotypeAtPosition ? GetMatchingHla(locus, position, resolution) : GetNonMatchingHla(locus, resolution);
        }

        private string GetMatchingHla(Locus locus, LocusPosition position, HlaTypingResolution resolution)
        {
            return genotype.Hla.GetPosition(locus, position).GetHlaForResolution(resolution);
        }

        private static string GetNonMatchingHla(Locus locus, HlaTypingResolution resolution)
        {
            var tgsAllele = TgsAllele.FromTestDataAllele(NonMatchingAlleles.NonMatchingDonorAlleles.GetLocus(locus));
            return tgsAllele.GetHlaForResolution(resolution);
        }
    }
}