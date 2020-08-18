using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence
{
    public interface IConfidenceCalculator
    {
        MatchConfidence CalculateConfidence(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata);
    }

    public class ConfidenceCalculator : IConfidenceCalculator
    {
        public MatchConfidence CalculateConfidence(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            // If either patient or donor is untyped, the match is potential
            if (patientMetadata == null || donorMetadata == null)
            {
                return MatchConfidence.Potential;
            }

            if (!IsMatch(patientMetadata, donorMetadata))
            {
                return MatchConfidence.Mismatch;
            }

            if (IsDefiniteMatch(patientMetadata, donorMetadata))
            {
                return MatchConfidence.Definite;
            }

            if (IsExactMatch(patientMetadata, donorMetadata))
            {
                return MatchConfidence.Exact;
            }

            return MatchConfidence.Potential;
        }

        private static bool IsMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            if (patientMetadata.HlaScoringInfo is SerologyScoringInfo ||
                donorMetadata.HlaScoringInfo is SerologyScoringInfo)
            {
                return IsSerologyMatch(patientMetadata, donorMetadata);
            }

            return IsMolecularMatch(patientMetadata, donorMetadata);
        }

        private static bool IsMolecularMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            var patientPGroups = patientMetadata.HlaScoringInfo.MatchingPGroups;
            var donorPGroups = donorMetadata.HlaScoringInfo.MatchingPGroups;

            return patientPGroups.Intersect(donorPGroups).Any();
        }

        private static bool IsSerologyMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            var patientSerologies = patientMetadata.HlaScoringInfo.MatchingSerologies.ToList();
            var donorSerologies = donorMetadata.HlaScoringInfo.MatchingSerologies.ToList();

            var isDirectMatch = AreSerologiesMatched(patientSerologies, true, donorSerologies, true);
            var isDonorIndirectMatch = AreSerologiesMatched(patientSerologies, true, donorSerologies, false);
            var isPatientIndirectMatch = AreSerologiesMatched(patientSerologies, false, donorSerologies, true);

            return isDirectMatch || isDonorIndirectMatch || isPatientIndirectMatch;
        }

        /// <summary>
        /// Determines if two sets of serologies are matched by their respective direct or indirect mappings.
        /// </summary>
        private static bool AreSerologiesMatched(
            IEnumerable<SerologyEntry> patientSerologies,
            bool usePatientDirectMapping,
            IEnumerable<SerologyEntry> donorSerologies,
            bool useDonorDirectMapping)
        {
            return
                patientSerologies.Where(s => s.IsDirectMapping == usePatientDirectMapping).Select(s => s.Name)
                    .Intersect(donorSerologies.Where(s => s.IsDirectMapping == useDonorDirectMapping).Select(s => s.Name))
                    .Any();
        }

        private static bool IsDefiniteMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return patientMetadata.HlaScoringInfo is SingleAlleleScoringInfo
                   && donorMetadata.HlaScoringInfo is SingleAlleleScoringInfo;
        }

        private static bool IsExactMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return !(patientMetadata.HlaScoringInfo is SerologyScoringInfo)
                   && !(donorMetadata.HlaScoringInfo is SerologyScoringInfo)
                   && patientMetadata.HlaScoringInfo.MatchingPGroups.Count() == 1
                   && donorMetadata.HlaScoringInfo.MatchingPGroups.Count() == 1;
        }
    }
}