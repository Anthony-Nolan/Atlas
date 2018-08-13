using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.Utils.Models;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data
    /// e.g. A 9/10 adult match with mismatch at A, from AN registry
    /// </summary>
    public class PatientDataSelector
    {
        private MetaDonor selectedMetaDonor;
        public bool HasMatch { get; set; }

        public List<DonorType> MatchingDonorTypes { get; } = new List<DonorType>();
        public List<RegistryCode> MatchingRegistries { get; } = new List<RegistryCode>();

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
        private readonly PhenotypeInfo<HlaTypingCategory> MatchingTypingCategories = new PhenotypeInfo<HlaTypingCategory>();

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
        }

        /// <summary>
        /// Will set the desired matching category at all positions
        /// </summary>
        public void SetFullMatchingTypingCategory(HlaTypingCategory category)
        {
            MatchingTypingCategories.A_1 = category;
            MatchingTypingCategories.A_2 = category;
            MatchingTypingCategories.B_1 = category;
            MatchingTypingCategories.B_2 = category;
            MatchingTypingCategories.C_1 = category;
            MatchingTypingCategories.C_2 = category;
            MatchingTypingCategories.DPB1_1 = category;
            MatchingTypingCategories.DPB1_2 = category;
            MatchingTypingCategories.DQB1_1 = category;
            MatchingTypingCategories.DQB1_2 = category;
            MatchingTypingCategories.DRB1_1 = category;
            MatchingTypingCategories.DRB1_2 = category;
        }

        public void SetMatchingDonorUntypedAtLocus(Locus locus)
        {
            MatchingTypingCategories.SetAtLocus(locus, TypePositions.Both, HlaTypingCategory.Untyped);
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
            var matchingMetaDonors = MetaDonorRepository.MetaDonors
                .Where(md => MatchingDonorTypes.Contains(md.DonorType))
                .Where(md => MatchingRegistries.Contains(md.Registry))
                .Where(md => MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup) || md.HasNonUniquePGroups);

            selectedMetaDonor = matchingMetaDonors.First();

            var matchingGenotype = new Genotype
            {
                Hla = selectedMetaDonor.Genotype.Hla.Map((l, p, hla) =>
                {
                    if (MatchLevels.DataAtPosition(l, p) == MatchLevel.PGroup)
                    {
                        var pGroup = AlleleRepository.FourFieldAlleles.DataAtPosition(l, p).First(a => a.AlleleName == hla.TgsTypedAllele).PGroup;
                        var selectedAllele = AlleleRepository.FourFieldAllelesWithNonUniquePGroups.DataAtPosition(l, p).First(a =>
                            a.PGroup == pGroup && a.AlleleName != hla.TgsTypedAllele);
                        return TgsAllele.FromFourFieldAllele(selectedAllele, l);
                    }

                    return hla;
                })
            };

            return matchingGenotype.Hla.Map((locus, position, tgsAllele) => HlaMatches.DataAtPosition(locus, position)
                ? tgsAllele.TgsTypedAllele
                : GenotypeRepository.NonMatchingGenotype.Hla.DataAtPosition(locus, position).TgsTypedAllele);
        }

        public int GetExpectedMatchingDonorId()
        {
            for (var i = 0; i < selectedMetaDonor.HlaTypingCategorySets.Count; i++)
            {
                if (selectedMetaDonor.HlaTypingCategorySets[i].Equals(MatchingTypingCategories))
                {
                    return selectedMetaDonor.DatabaseDonors[i].DonorId;
                }
            }

            throw new Exception("Failed to find the expected matched donor for this patient - does the corresponding test data exist?");
        }
    }
}