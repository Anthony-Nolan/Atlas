using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Collections.Generic;
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
            var patientPGroups = GetPGroups(patientLookupResult);
            var donorPGroups = GetPGroups(donorLookupResult);

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
                   && GetPGroups(patientLookupResult).Count() == 1
                   && GetPGroups(donorLookupResult).Count() == 1;
        }

        private static IEnumerable<string> GetPGroups(IHlaScoringLookupResult patientLookupResult)
        {
            switch (patientLookupResult.HlaScoringInfo)
            {
                case SerologyScoringInfo _:
                    throw new Exception("Cannot compare p-groups for serology scoring info - compare matching serologies instead");
                case ConsolidatedMolecularScoringInfo consolidatedMolecularInfo:
                    return consolidatedMolecularInfo.MatchingPGroups;
                case SingleAlleleScoringInfo singleAlleleInfo:
                    return new List<string> {singleAlleleInfo.MatchingPGroup};
                case MultipleAlleleScoringInfo multipleAlleleInfo:
                    return multipleAlleleInfo.AlleleScoringInfos.Select(alleleInfo => alleleInfo.MatchingPGroup);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}