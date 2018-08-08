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
        public bool HasMatch { get; set; }
        
        public PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();

        public List<DonorType> MatchingDonorTypes { get; set; } = new List<DonorType>();
        public List<RegistryCode> MatchingRegistries { get; set; } = new List<RegistryCode>();
        public List<HlaTypingCategory> MatchingTypingCategories { get; set; } = new List<HlaTypingCategory>();

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            var matchingGenotype = GenotypeRepository.Genotypes.First();

            return matchingGenotype.Hla.Map((locus, position, tgsAllele) => HlaMatches.DataAtPosition(locus, position)
                ? tgsAllele.TgsTypedAllele
                : GenotypeRepository.NonMatchingGenotype.Hla.DataAtPosition(locus, position).TgsTypedAllele);
        }
    }
}