using Nova.SearchAlgorithm.Client.Models.SearchResults;
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
            // if patient and/or donor type are missing, then match grade
            // is automatically P group
            if (patientLookupResult == null || donorLookupResult == null)
            {
                return MatchGrade.PGroup;
            }

            if (patientLookupResult.MatchLocus != donorLookupResult.MatchLocus)
            {
                throw new ArgumentException("Lookup results do not belong to same locus.");
            }

            if (!ScoringInfosAreOfPermittedTypes(
                patientLookupResult.HlaScoringInfo, 
                donorLookupResult.HlaScoringInfo))
            {
                throw new ArgumentException("One or both scoring infos are not of the permitted types.");
            }

            return GetMatchGrade(patientLookupResult, donorLookupResult);
        }

        protected abstract bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo);

        protected abstract MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult);
    }
}