using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using System;

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
            donor = new Donor 
            {
                DonorId = DonorIdGenerator.NextId(),
                ExternalDonorCode = DonorIdGenerator.NewExternalCode
            };
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
            donor.A_1 = GetHla(Locus.A, LocusPosition.One);
            donor.A_2 = GetHla(Locus.A, LocusPosition.Two);
            donor.B_1 = GetHla(Locus.B, LocusPosition.One);
            donor.B_2 = GetHla(Locus.B, LocusPosition.Two);
            donor.C_1 = GetHla(Locus.C, LocusPosition.One);
            donor.C_2 = GetHla(Locus.C, LocusPosition.Two);
            donor.DPB1_1 = GetHla(Locus.Dpb1, LocusPosition.One);
            donor.DPB1_2 = GetHla(Locus.Dpb1, LocusPosition.Two);
            donor.DQB1_1 = GetHla(Locus.Dqb1, LocusPosition.One);
            donor.DQB1_2 = GetHla(Locus.Dqb1, LocusPosition.Two);
            donor.DRB1_1 = GetHla(Locus.Drb1, LocusPosition.One);
            donor.DRB1_2 = GetHla(Locus.Drb1, LocusPosition.Two);

            // TODO: ATLAS-964: Resolve this situation more elegantly
            if (donor.DPB1_1 == null ^ donor.DPB1_2 == null)
            {
                // There are some scenarios for which we just don't have any DPB1 data available to fulfil the specified donor resolution. 
                // As a donor with a partially typed locus will be rejected by the algorithm, in these cases it is better to completely remove DPB1 typing for such donors. 
                donor.DPB1_1 = null;
                donor.DPB1_2 = null;
            }
            
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