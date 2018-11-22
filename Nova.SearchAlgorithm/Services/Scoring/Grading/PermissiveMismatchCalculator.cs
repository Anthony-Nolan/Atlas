using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
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
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;

        public PermissiveMismatchCalculator(IDpb1TceGroupLookupService dpb1TceGroupLookupService)
        {
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
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
            return dpb1TceGroupLookupService.GetDpb1TceGroup(alleleName);
        }
    }
}