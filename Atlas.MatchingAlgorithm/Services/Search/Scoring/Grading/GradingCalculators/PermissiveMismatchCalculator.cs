using Atlas.MatchingAlgorithm.Common.Models;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;

namespace Atlas.MatchingAlgorithm.Services.Scoring.Grading
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
            IActiveHlaVersionAccessor hlaVersionAccessor)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaVersionAccessor.GetActiveHlaDatabaseVersion());
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