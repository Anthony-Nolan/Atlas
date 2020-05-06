using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Linq;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;

namespace Atlas.MatchingAlgorithm.Services.Scoring.Confidence
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades);
    }

    public class ConfidenceService : IConfidenceService
    {
        private readonly IConfidenceCalculator confidenceCalculator;
        private readonly IScoringCache scoringCache;

        public ConfidenceService(IConfidenceCalculator confidenceCalculator, IScoringCache scoringCache)
        {
            this.confidenceCalculator = confidenceCalculator;
            this.scoringCache = scoringCache;
        }

        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            var confidenceResults = new PhenotypeInfo<MatchConfidence>();

            patientLookupResults.EachLocus((locus, patientLookupResult1, patientLookupResult2) =>
            {
                var matchGradesAtLocus = matchGrades.DataAtLocus(locus);
                var orientations = matchGradesAtLocus.Item1.Orientations;

                var confidences = orientations.Select(o => new Tuple<MatchConfidence, MatchConfidence>(
                    CalculateConfidenceForOrientation(locus, TypePosition.One, patientLookupResult1, donorLookupResults, o),
                    CalculateConfidenceForOrientation(locus, TypePosition.Two, patientLookupResult2, donorLookupResults, o)
                ));

                // In the case where the best grade for a donor is the same for both a cross and direct match, but the confidence for each is different,
                // We should return the best confidence amongst orientations provided
                var selectedConfidences = confidences
                    .OrderByDescending(c => (int) c.Item1 + (int) c.Item2)
                    .First();

                confidenceResults.SetAtPosition(locus, TypePosition.One, selectedConfidences.Item1);
                confidenceResults.SetAtPosition(locus, TypePosition.Two, selectedConfidences.Item2);
            });

            return confidenceResults;
        }

        private MatchConfidence CalculateConfidenceForOrientation(
            Locus locus,
            TypePosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            MatchOrientation matchOrientation)
        {
            switch (matchOrientation)
            {
                case MatchOrientation.Direct:
                    return CalculateConfidenceForDirectMatch(locus, position, patientLookupResult, donorLookupResults);
                case MatchOrientation.Cross:
                    return CalculateConfidenceForCrossMatch(locus, position, patientLookupResult, donorLookupResults);
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchOrientation), matchOrientation, null);
            }
        }

        private MatchConfidence CalculateConfidenceForDirectMatch(
            Locus locus,
            TypePosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            return GetConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, position));
        }

        private MatchConfidence CalculateConfidenceForCrossMatch(
            Locus locus,
            TypePosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            switch (position)
            {
                case TypePosition.One:
                    return GetConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePosition.Two));
                case TypePosition.Two:
                    return GetConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePosition.One));
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        private MatchConfidence GetConfidence(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResults)
        {
            return scoringCache.GetOrAddMatchConfidence(
                patientLookupResult?.Locus,
                patientLookupResult?.LookupName,
                donorLookupResults?.LookupName,
                c => confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults));
        }
    }
}