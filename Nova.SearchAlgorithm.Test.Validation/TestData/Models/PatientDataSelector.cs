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

        public PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();

        public List<DonorType> MatchingDonorTypes { get; set; } = new List<DonorType>();
        public List<RegistryCode> MatchingRegistries { get; set; } = new List<RegistryCode>();
        public HlaTypingCategory MatchingTypingCategory { get; set; }

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
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
                var typingCategorySet = selectedMetaDonor.HlaTypingCategorySets[i];
                if (IsTypingCategoryAtAllPositions(typingCategorySet, MatchingTypingCategory))
                {
                    return selectedMetaDonor.DatabaseDonors[i].DonorId;
                }
            }
            throw new Exception("Failed to find the expected matched donor for this patient.");
        }

        private static bool IsTypingCategoryAtAllPositions(PhenotypeInfo<HlaTypingCategory> typingCategorySet, HlaTypingCategory typingCategory)
        {
            return typingCategorySet.A_1 == typingCategory
                   && typingCategorySet.A_2 == typingCategory
                   && typingCategorySet.B_1 == typingCategory
                   && typingCategorySet.B_2 == typingCategory
                   && typingCategorySet.C_1 == typingCategory
                   && typingCategorySet.C_2 == typingCategory
                   && typingCategorySet.DRB1_1 == typingCategory
                   && typingCategorySet.DRB1_2 == typingCategory
                   && typingCategorySet.DQB1_1 == typingCategory
                   && typingCategorySet.DQB1_2 == typingCategory
                   && typingCategorySet.DPB1_1 == typingCategory
                   && typingCategorySet.DPB1_2 == typingCategory;
        }
    }
}