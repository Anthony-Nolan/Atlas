using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public abstract class GradingCalculatorBase : IGradingCalculator
    {
        public MatchGrade CalculateGrade(
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult)
        {
            if (patientLookupResult == null || donorLookupResult == null)
            {
                throw new ArgumentException("One or both lookup results are null.");
            }

            if (patientLookupResult.MatchLocus != donorLookupResult.MatchLocus)
            {
                throw new ArgumentException("Lookup results do not belong to same locus.");
            }

            if (!ScoringInfosAreOfPermittedTypes(patientLookupResult, donorLookupResult))
            {
                throw new ArgumentException("One or both scoring infos are not of the permitted types.");
            }

            return GetMatchGrade(patientLookupResult, donorLookupResult);
        }

        protected abstract bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult);

        protected abstract MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult);
    }
}