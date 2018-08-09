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
        private GenotypeDonor selectedMetaDonor;
        public bool HasMatch { get; set; }

        public List<DonorType> MatchingDonorTypes { get; } = new List<DonorType>();
        public List<RegistryCode> MatchingRegistries { get; } = new List<RegistryCode>();

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
        
        public PhenotypeInfo<string> GetPatientHla()
        {
            var matchingMetaDonors = MetaDonorRepository.MetaDonors.Where(md =>
                MatchingDonorTypes.Contains(md.DonorType)
                && MatchingRegistries.Contains(md.Registry));

            selectedMetaDonor = matchingMetaDonors.First();
            
            var matchingGenotype = selectedMetaDonor.Genotype;

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