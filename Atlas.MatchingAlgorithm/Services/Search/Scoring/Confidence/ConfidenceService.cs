using System;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence
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

            patientLookupResults.EachLocus((locus, patientLookupResultsAtLocus) =>
            {
                var matchGradesAtLocus = matchGrades.GetLocus(locus);
                var orientations = matchGradesAtLocus.Position1.Orientations;

                var confidences = orientations.Select(o => new LocusInfo<MatchConfidence>(
                    CalculateConfidenceForOrientation(locus, LocusPosition.One, patientLookupResultsAtLocus.Position1, donorLookupResults, o),
                    CalculateConfidenceForOrientation(locus, LocusPosition.Two, patientLookupResultsAtLocus.Position2, donorLookupResults, o)
                ));

                // In the case where the best grade for a donor is the same for both a cross and direct match, but the confidence for each is different,
                // We should return the best confidence amongst orientations provided
                var selectedConfidences = confidences
                    .OrderByDescending(c => (int) c.Position1 + (int) c.Position2)
                    .First();

                confidenceResults.SetPosition(locus, LocusPosition.One, selectedConfidences.Position1);
                confidenceResults.SetPosition(locus, LocusPosition.Two, selectedConfidences.Position2);
            });

            return confidenceResults;
        }

        private MatchConfidence CalculateConfidenceForOrientation(
            Locus locus,
            LocusPosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            MatchOrientation matchOrientation)
        {
            return matchOrientation switch
            {
                MatchOrientation.Direct => CalculateConfidenceForDirectMatch(locus, position, patientLookupResult, donorLookupResults),
                MatchOrientation.Cross => CalculateConfidenceForCrossMatch(locus, position, patientLookupResult, donorLookupResults),
                _ => throw new ArgumentOutOfRangeException(nameof(matchOrientation), matchOrientation, null)
            };
        }

        private MatchConfidence CalculateConfidenceForDirectMatch(
            Locus locus,
            LocusPosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            return GetConfidence(patientLookupResult, donorLookupResults.GetPosition(locus, position));
        }

        private MatchConfidence CalculateConfidenceForCrossMatch(
            Locus locus,
            LocusPosition position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            return position switch
            {
                LocusPosition.One => GetConfidence(patientLookupResult, donorLookupResults.GetPosition(locus, LocusPosition.Two)),
                LocusPosition.Two => GetConfidence(patientLookupResult, donorLookupResults.GetPosition(locus, LocusPosition.One)),
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
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