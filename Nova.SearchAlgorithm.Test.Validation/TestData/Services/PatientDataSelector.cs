using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data
    /// e.g. A 9/10 adult match with mismatch at A, from AN registry
    /// </summary>
    public class PatientDataSelector
    {
        private readonly IMetaDonorRepository metaDonorRepository;
        private readonly IAlleleRepository alleleRepository;

        public PatientDataSelector(IMetaDonorRepository metaDonorRepository, IAlleleRepository alleleRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
            this.alleleRepository = alleleRepository;
        }
        
        private MetaDonor selectedMetaDonor;

        /// <summary>
        /// The match level of the expected matching donor (if HasMatch == true)
        /// e.g. If PGroup, an different allele in the same p-group as the donor will be selected
        /// </summary>
        private PhenotypeInfo<MatchLevel> MatchLevels { get; } = new PhenotypeInfo<MatchLevel>
        {
            A_1 = MatchLevel.Allele,
            A_2 = MatchLevel.Allele,
            B_1 = MatchLevel.Allele,
            B_2 = MatchLevel.Allele,
            C_1 = MatchLevel.Allele,
            C_2 = MatchLevel.Allele,
            DPB1_1 = MatchLevel.Allele,
            DPB1_2 = MatchLevel.Allele,
            DQB1_1 = MatchLevel.Allele,
            DQB1_2 = MatchLevel.Allele,
            DRB1_1 = MatchLevel.Allele,
            DRB1_2 = MatchLevel.Allele,
        };

        private PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();

        /// <summary>
        /// Determines to what resolution the expected matched donor is typed
        /// </summary>
        private readonly PhenotypeInfo<HlaTypingResolution> matchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>();
        
        /// <summary>
        /// Determines how many fields the matching meta-donor's genotype should have at each position
        /// </summary>
        private readonly PhenotypeInfo<TgsHlaTypingCategory> matchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>();

        public bool HasMatch { get; set; }

        public List<DonorType> MatchingDonorTypes { get; } = new List<DonorType>();

        public List<RegistryCode> MatchingRegistries { get; } = new List<RegistryCode>();

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
        }

        /// <summary>
        /// Will set the desired matching category at all positions
        /// </summary>
        public void SetFullMatchingTypingResolution(HlaTypingResolution resolution)
        {
            matchingTypingResolutions.A_1 = resolution;
            matchingTypingResolutions.A_2 = resolution;
            matchingTypingResolutions.B_1 = resolution;
            matchingTypingResolutions.B_2 = resolution;
            matchingTypingResolutions.C_1 = resolution;
            matchingTypingResolutions.C_2 = resolution;
            matchingTypingResolutions.DPB1_1 = resolution;
            matchingTypingResolutions.DPB1_2 = resolution;
            matchingTypingResolutions.DQB1_1 = resolution;
            matchingTypingResolutions.DQB1_2 = resolution;
            matchingTypingResolutions.DRB1_1 = resolution;
            matchingTypingResolutions.DRB1_2 = resolution;
        }
        
        /// <summary>
        /// Will set the desired matching category at all positions
        /// </summary>
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            matchingTgsTypingCategories.A_1 = tgsCategory;
            matchingTgsTypingCategories.A_2 = tgsCategory;
            matchingTgsTypingCategories.B_1 = tgsCategory;
            matchingTgsTypingCategories.B_2 = tgsCategory;
            matchingTgsTypingCategories.C_1 = tgsCategory;
            matchingTgsTypingCategories.C_2 = tgsCategory;
            matchingTgsTypingCategories.DPB1_1 = tgsCategory;
            matchingTgsTypingCategories.DPB1_2 = tgsCategory;
            matchingTgsTypingCategories.DQB1_1 = tgsCategory;
            matchingTgsTypingCategories.DQB1_2 = tgsCategory;
            matchingTgsTypingCategories.DRB1_1 = tgsCategory;
            matchingTgsTypingCategories.DRB1_2 = tgsCategory;
        }

        public void SetMatchingDonorUntypedAtLocus(Locus locus)
        {
            matchingTypingResolutions.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            MatchLevels.A_1 = matchLevel;
            MatchLevels.A_2 = matchLevel;
            MatchLevels.B_1 = matchLevel;
            MatchLevels.B_2 = matchLevel;
            MatchLevels.C_1 = matchLevel;
            MatchLevels.C_2 = matchLevel;
            MatchLevels.DPB1_1 = matchLevel;
            MatchLevels.DPB1_2 = matchLevel;
            MatchLevels.DQB1_1 = matchLevel;
            MatchLevels.DQB1_2 = matchLevel;
            MatchLevels.DRB1_1 = matchLevel;
            MatchLevels.DRB1_2 = matchLevel;
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            var matchingMetaDonors = metaDonorRepository.AllMetaDonors()
                .Where(md => MatchingDonorTypes.Contains(md.DonorType))
                .Where(md => MatchingRegistries.Contains(md.Registry))
                .Where(md => MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup)
                             || md.GenotypeCriteria.HasNonUniquePGroups.ToEnumerable().Any(x => x));

            selectedMetaDonor = matchingMetaDonors.First();

            var matchingGenotype = new Genotype
            {
                Hla = selectedMetaDonor.Genotype.Hla.Map((l, p, hla) =>
                {
                    if (MatchLevels.DataAtPosition(l, p) == MatchLevel.PGroup)
                    {
                        var allelesAtLocus = alleleRepository.FourFieldAllelesWithNonUniquePGroups().DataAtLocus(l);
                        var allAllelesAtLocus = allelesAtLocus.Item1.Concat(allelesAtLocus.Item2).ToList();
                        var pGroup = allAllelesAtLocus.First(a => a.AlleleName == hla.TgsTypedAllele).PGroup;
                        var selectedAllele = allAllelesAtLocus.First(a =>
                            a.PGroup == pGroup
                            && a.AlleleName != hla.TgsTypedAllele
                            && a.AlleleName != selectedMetaDonor.Genotype.Hla.DataAtPosition(l, p.Other()).TgsTypedAllele);
                        return TgsAllele.FromFourFieldAllele(selectedAllele, l);
                    }

                    return hla;
                })
            };

            return matchingGenotype.Hla.Map((locus, position, tgsAllele) => HlaMatches.DataAtPosition(locus, position)
                ? tgsAllele.TgsTypedAllele
                : GenotypeGenerator.NonMatchingGenotype.Hla.DataAtPosition(locus, position).TgsTypedAllele);
        }

        public int GetExpectedMatchingDonorId()
        {
            for (var i = 0; i < selectedMetaDonor.HlaTypingCategorySets.Count; i++)
            {
                if (selectedMetaDonor.HlaTypingCategorySets[i].Equals(matchingTypingResolutions))
                {
                    return selectedMetaDonor.DatabaseDonors[i].DonorId;
                }
            }

            throw new Exception("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }
    }
}