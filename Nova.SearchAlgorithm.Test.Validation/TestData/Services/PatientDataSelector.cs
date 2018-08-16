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
        public bool HasMatch { get; set; }
        public List<DonorType> MatchingDonorTypes { get; } = new List<DonorType>();
        public List<RegistryCode> MatchingRegistries { get; } = new List<RegistryCode>();

        private readonly IMetaDonorRepository metaDonorRepository;
        private readonly IAlleleRepository alleleRepository;        
        private MetaDonor selectedMetaDonor;

        private PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();

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

        // TODO: NOVA-1642 - patient typing resolutions to be set by step; currently defaulting to TGS
        private readonly PhenotypeInfo<HlaTypingResolution> patientTypingCategories = new PhenotypeInfo<HlaTypingResolution>
        {
            A_1 = HlaTypingResolution.Tgs,
            A_2 = HlaTypingResolution.Tgs,
            B_1 = HlaTypingResolution.Tgs,
            B_2 = HlaTypingResolution.Tgs,
            C_1 = HlaTypingResolution.Tgs,
            C_2 = HlaTypingResolution.Tgs,
            DPB1_1 = HlaTypingResolution.Tgs,
            DPB1_2 = HlaTypingResolution.Tgs,
            DQB1_1 = HlaTypingResolution.Tgs,
            DQB1_2 = HlaTypingResolution.Tgs,
            DRB1_1 = HlaTypingResolution.Tgs,
            DRB1_2 = HlaTypingResolution.Tgs,
        };

        /// <summary>
        /// Determines to what resolution the expected matched donor is typed
        /// </summary>
        private readonly PhenotypeInfo<HlaTypingResolution> matchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>();
        
        /// <summary>
        /// Determines how many fields the matching meta-donor's genotype should have at each position
        /// </summary>
        private readonly PhenotypeInfo<TgsHlaTypingCategory> matchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>();

        public PatientDataSelector(IMetaDonorRepository metaDonorRepository, IAlleleRepository alleleRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
            this.alleleRepository = alleleRepository;
        }

        public void SetPatientUntypedAt(Locus locus)
        {
            patientTypingCategories.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
        }

        public void SetAsEightOutOfEightMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dqb1 || l != Locus.Dpb1);
        }

        public void SetAsSixOutOfSixMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.C || l != Locus.Dqb1 || l != Locus.Dpb1);
        }

        /// <summary>
        /// Will set the desired typing resolution at all positions
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
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            matchingTgsTypingCategories.A_1 = tgsCategory;
            matchingTgsTypingCategories.A_2 = tgsCategory;
            matchingTgsTypingCategories.B_1 = tgsCategory;
            matchingTgsTypingCategories.B_2 = tgsCategory;
            matchingTgsTypingCategories.C_1 = tgsCategory;
            matchingTgsTypingCategories.C_2 = tgsCategory;
            // There is no DPB1 test data with fewer than 4 fields
            matchingTgsTypingCategories.DPB1_1 = TgsHlaTypingCategory.FourFieldAllele;
            matchingTgsTypingCategories.DPB1_2 = TgsHlaTypingCategory.FourFieldAllele;
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
            return GetMetaDonor().Genotype.Hla.Map(GetHlaName);
        }

        public int GetExpectedMatchingDonorId()
        {
            var metaDonor = GetMetaDonor();
            for (var i = 0; i < metaDonor.HlaTypingResolutionSets.Count; i++)
            {
                if (metaDonor.HlaTypingResolutionSets[i].Equals(matchingTypingResolutions))
                {
                    return metaDonor.DatabaseDonors[i].DonorId;
                }
            }

            throw new Exception("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }

        private MetaDonor GetMetaDonor()
        {
            // Cache the selected meta-donor to ensure we do not have to perform this calculation multiple times
            if (selectedMetaDonor == null)
            {
                var matchingMetaDonors = metaDonorRepository.AllMetaDonors()
                    .Where(md => MatchingDonorTypes.Contains(md.DonorType))
                    .Where(md => MatchingRegistries.Contains(md.Registry))
                    .Where(md => matchingTgsTypingCategories.Equals(md.GenotypeCriteria.TgsHlaCategories))
                    .Where(md => MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup)
                                 || md.GenotypeCriteria.HasNonUniquePGroups.ToEnumerable().Any(x => x));
                selectedMetaDonor = matchingMetaDonors.First();
            }

            return selectedMetaDonor;
        }

        private string GetHlaName(Locus locus, TypePositions position, TgsAllele tgsAllele)
        {
            var allele = GetTgsAllele(locus, position, tgsAllele);
            var typingCategory = patientTypingCategories.DataAtPosition(locus, position);

            return allele.GetHlaForCategory(typingCategory);
        }

        private TgsAllele GetTgsAllele(Locus locus, TypePositions position, TgsAllele originalAllele)
        {
            // if patient should be mismatched at this position
            if (!HlaMatches.DataAtPosition(locus, position))
            {
                return GenotypeGenerator.NonMatchingGenotype.Hla.DataAtPosition(locus, position);
            }

            // if patient should have a P-group match at this position
            if (MatchLevels.DataAtPosition(locus, position) == MatchLevel.PGroup)
            {
                return GetDifferentTgsAlleleFromSamePGroup(locus, originalAllele, position);
            }

            return originalAllele;
        }
        
        private TgsAllele GetDifferentTgsAlleleFromSamePGroup(Locus locus, TgsAllele allele, TypePositions position)
        {
            var allelesAtLocus = alleleRepository.FourFieldAllelesWithNonUniquePGroups().DataAtLocus(locus);
            var allAllelesAtLocus = allelesAtLocus.Item1.Concat(allelesAtLocus.Item2).ToList();
            var pGroup = allAllelesAtLocus.First(a => a.AlleleName == allele.TgsTypedAllele).PGroup;
            var selectedAllele = allAllelesAtLocus.First(a =>
                a.PGroup == pGroup
                && a.AlleleName != allele.TgsTypedAllele
                && a.AlleleName != GetMetaDonor().Genotype.Hla.DataAtPosition(locus, position.Other()).TgsTypedAllele);

            return TgsAllele.FromFourFieldAllele(selectedAllele, locus);
        }
    }
}