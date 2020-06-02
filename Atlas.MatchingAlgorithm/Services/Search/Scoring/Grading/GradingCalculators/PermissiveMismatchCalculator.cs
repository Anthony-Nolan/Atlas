using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    /// <summary>
    /// Calculates whether a patient and donor molecular HLA typing are
    /// permissively mismatched.
    /// </summary>
    public interface IPermissiveMismatchCalculator
    {
        bool IsPermissiveMismatch(Locus locus, string patientHlaName, string donorHlaName);
    }

    public class PermissiveMismatchCalculator : IPermissiveMismatchCalculator
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public PermissiveMismatchCalculator(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public bool IsPermissiveMismatch(Locus locus, string patientHlaName, string donorHlaName)
        {
            return locus == Locus.Dpb1 && IsDpb1PermissiveMismatch(patientHlaName, donorHlaName);
        }

        private bool IsDpb1PermissiveMismatch(string patientHlaName, string donorHlaName)
        {
            var patientTceGroup = GetDpb1TceGroup(patientHlaName).Result;
            var donorTceGroup = GetDpb1TceGroup(donorHlaName).Result;

            return
                !string.IsNullOrEmpty(patientTceGroup) &&
                !string.IsNullOrEmpty(donorTceGroup) &&
                string.Equals(patientTceGroup, donorTceGroup);
        }

        private Task<string> GetDpb1TceGroup(string alleleName)
        {
            return hlaMetadataDictionary.GetDpb1TceGroup(alleleName);
        }
    }
}