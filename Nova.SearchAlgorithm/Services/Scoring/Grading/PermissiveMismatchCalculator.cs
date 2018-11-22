using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// Calculates whether a patient and donor molecular HLA typing are
    /// permissively mismatched.
    /// </summary>
    public interface IPermissiveMismatchCalculator
    {
        bool IsPermissiveMismatched(
            MatchLocus matchLocus, 
            string patientHlaName,
            string donorHlaName);
    }

    public class PermissiveMismatchCalculator : IPermissiveMismatchCalculator
    {
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;

        public PermissiveMismatchCalculator(IDpb1TceGroupLookupService dpb1TceGroupLookupService)
        {
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
        }

        public bool IsPermissiveMismatched(MatchLocus matchLocus, string patientHlaName, string donorHlaName)
        {
            throw new System.NotImplementedException();
        }
    }
}