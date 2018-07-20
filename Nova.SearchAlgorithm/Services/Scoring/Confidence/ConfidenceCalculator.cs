using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

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
            if (patientLookupResult.TypingMethod == TypingMethod.Serology || donorLookupResult.TypingMethod == TypingMethod.Serology)
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
            var patientSerologies = GetSerologyEntries(patientLookupResult);
            var donorSerologies = GetSerologyEntries(donorLookupResult);

            return patientSerologies.Intersect(donorSerologies).Any();
        }

        private static bool IsDefiniteMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            return patientLookupResult.HlaScoringInfo is SingleAlleleScoringInfo
                   && donorLookupResult.HlaScoringInfo is SingleAlleleScoringInfo;
        }

        private static bool IsExactMatch(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            return patientLookupResult.TypingMethod == TypingMethod.Molecular
                   && donorLookupResult.TypingMethod == TypingMethod.Molecular
                   && GetPGroups(patientLookupResult).Count() == 1
                   && GetPGroups(donorLookupResult).Count() == 1;
        }

        private static IEnumerable<SerologyEntry> GetSerologyEntries(IHlaScoringLookupResult patientLookupResult)
        {
            switch (patientLookupResult.HlaScoringInfo)
            {
                case SerologyScoringInfo serologyInfo:
                    return serologyInfo.MatchingSerologies;
                case XxCodeScoringInfo xxInfo:
                    return xxInfo.MatchingSerologies;
                case SingleAlleleScoringInfo singleAlleleInfo:
                    return singleAlleleInfo.MatchingSerologies;
                case MultipleAlleleScoringInfo multipleAlleleInfo:
                    return multipleAlleleInfo.AlleleScoringInfos.SelectMany(alleleInfo => alleleInfo.MatchingSerologies);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IEnumerable<string> GetPGroups(IHlaScoringLookupResult patientLookupResult)
        {
            switch (patientLookupResult.HlaScoringInfo)
            {
                case SerologyScoringInfo serologyScoringInfo:
                    throw new Exception("Cannot compare p-groups for serology scoring info - compare matching serologies instead");
                case XxCodeScoringInfo xxInfo:
                    return xxInfo.MatchingPGroups;
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