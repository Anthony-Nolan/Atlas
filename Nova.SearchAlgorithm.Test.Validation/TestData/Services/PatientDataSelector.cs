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

        // todo: NOVA-1642 - patient typing categories to be set by step; currently defaulting to TGS four field
        private readonly PhenotypeInfo<HlaTypingCategory> patientTypingCategories = new PhenotypeInfo<HlaTypingCategory>
        {
            A_1 = HlaTypingCategory.TgsFourFieldAllele,
            A_2 = HlaTypingCategory.TgsFourFieldAllele,
            B_1 = HlaTypingCategory.TgsFourFieldAllele,
            B_2 = HlaTypingCategory.TgsFourFieldAllele,
            C_1 = HlaTypingCategory.TgsFourFieldAllele,
            C_2 = HlaTypingCategory.TgsFourFieldAllele,
            DPB1_1 = HlaTypingCategory.TgsFourFieldAllele,
            DPB1_2 = HlaTypingCategory.TgsFourFieldAllele,
            DQB1_1 = HlaTypingCategory.TgsFourFieldAllele,
            DQB1_2 = HlaTypingCategory.TgsFourFieldAllele,
            DRB1_1 = HlaTypingCategory.TgsFourFieldAllele,
            DRB1_2 = HlaTypingCategory.TgsFourFieldAllele
        };

        private readonly PhenotypeInfo<HlaTypingCategory> matchingTypingCategories = new PhenotypeInfo<HlaTypingCategory>();

        public PatientDataSelector(IMetaDonorRepository metaDonorRepository, IAlleleRepository alleleRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
            this.alleleRepository = alleleRepository;
        }

        public void SetPatientUntypedAt(Locus locus)
        {
            patientTypingCategories.SetAtLocus(locus, TypePositions.Both, HlaTypingCategory.Untyped);
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
        /// Will set the desired matching category at all positions
        /// </summary>
        public void SetFullMatchingTypingCategory(HlaTypingCategory category)
        {
            matchingTypingCategories.A_1 = category;
            matchingTypingCategories.A_2 = category;
            matchingTypingCategories.B_1 = category;
            matchingTypingCategories.B_2 = category;
            matchingTypingCategories.C_1 = category;
            matchingTypingCategories.C_2 = category;
            matchingTypingCategories.DPB1_1 = category;
            matchingTypingCategories.DPB1_2 = category;
            matchingTypingCategories.DQB1_1 = category;
            matchingTypingCategories.DQB1_2 = category;
            matchingTypingCategories.DRB1_1 = category;
            matchingTypingCategories.DRB1_2 = category;
        }

        public void SetMatchingDonorUntypedAtLocus(Locus locus)
        {
            matchingTypingCategories.SetAtLocus(locus, TypePositions.Both, HlaTypingCategory.Untyped);
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
            selectedMetaDonor = GetMetaDonor();

            return selectedMetaDonor.Genotype.Hla.Map(GetHlaName);
        }

        public int GetExpectedMatchingDonorId()
        {
            for (var i = 0; i < selectedMetaDonor.HlaTypingCategorySets.Count; i++)
            {
                if (selectedMetaDonor.HlaTypingCategorySets[i].Equals(matchingTypingCategories))
                {
                    return selectedMetaDonor.DatabaseDonors[i].DonorId;
                }
            }

            throw new Exception("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }

        private MetaDonor GetMetaDonor()
        {
            var matchingMetaDonors = metaDonorRepository.AllMetaDonors()
                .Where(md => MatchingDonorTypes.Contains(md.DonorType))
                .Where(md => MatchingRegistries.Contains(md.Registry))
                .Where(md => MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup)
                             || md.GenotypeCriteria.HasNonUniquePGroups.ToEnumerable().Any(x => x));

            return matchingMetaDonors.First();
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
                && a.AlleleName != selectedMetaDonor.Genotype.Hla.DataAtPosition(locus, position.Other()).TgsTypedAllele);

            return TgsAllele.FromFourFieldAllele(selectedAllele, locus);
        }
    }
}