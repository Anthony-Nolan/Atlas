using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System.Linq;

namespace Nova.SearchAlgorithm.Services.Scoring.Confidence
{
    public interface IConfidenceCalculator
    {
        MatchConfidence CalculateConfidence(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult);
    }

    public class ConfidenceCalculator: IConfidenceCalculator
    {
        public MatchConfidence CalculateConfidence(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            // If either patient or donor is untyped, the match is potential
            if (patientLookupResult == null || donorLookupResult == null)
            {
                return MatchConfidence.Potential;
            }

            if (!IsMatch(patientLookupResult, donorLookupResult))
            {
                return MatchConfidence.Mismatch;
            }

            if (IsDefiniteMatch(patientLookupResult, donorLookupResult))
            {
                return MatchConfidence.Definite;
            }

            if (IsExactMatch(patientLookupResult, donorLookupResult))
            {
                return MatchConfidence.Exact;
            }

            return MatchConfidence.Potential;
        }

        private static bool IsMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            if (patientLookupResult.HlaScoringInfo is SerologyScoringInfo ||
                donorLookupResult.HlaScoringInfo is SerologyScoringInfo)
            {
                return IsSerologyMatch(patientLookupResult, donorLookupResult);
            }

            return IsMolecularMatch(patientLookupResult, donorLookupResult);
        }

        private static bool IsMolecularMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            var patientPGroups = patientLookupResult.HlaScoringInfo.MatchingPGroups;
            var donorPGroups = donorLookupResult.HlaScoringInfo.MatchingPGroups;

            return patientPGroups.Intersect(donorPGroups).Any();
        }

        private static bool IsSerologyMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            var patientSerologies = patientLookupResult.HlaScoringInfo.MatchingSerologies;
            var donorSerologies = donorLookupResult.HlaScoringInfo.MatchingSerologies;

            return patientSerologies.Intersect(donorSerologies).Any();
        }

        private static bool IsDefiniteMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            return patientLookupResult.HlaScoringInfo is SingleAlleleScoringInfo
                   && donorLookupResult.HlaScoringInfo is SingleAlleleScoringInfo;
        }

        private static bool IsExactMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            return !(patientLookupResult.HlaScoringInfo is SerologyScoringInfo)
                   && !(donorLookupResult.HlaScoringInfo is SerologyScoringInfo)
                   && patientLookupResult.HlaScoringInfo.MatchingPGroups.Count() == 1
                   && donorLookupResult.HlaScoringInfo.MatchingPGroups.Count() == 1;
        }
    }
}