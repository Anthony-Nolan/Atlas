using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Confidence
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

        public ConfidenceService(IConfidenceCalculator confidenceCalculator)
        {
            this.confidenceCalculator = confidenceCalculator;
        }

        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            var confidenceResults = new PhenotypeInfo<MatchConfidence>();

            patientLookupResults.EachLocus((locus, patientLookupResult1, patinetLookupResult2) =>
            {
                var matchGradesAtLocus = matchGrades.DataAtLocus(locus);
                var orientations = matchGradesAtLocus.Item1.Orientations;

                var confidences = orientations.Select(o => new Tuple<MatchConfidence, MatchConfidence>(
                    CalculateConfidenceForOrientation(locus, TypePositions.One, patientLookupResult1, donorLookupResults, o),
                    CalculateConfidenceForOrientation(locus, TypePositions.Two, patinetLookupResult2, donorLookupResults, o)
                ));

                // In the case where the best grade for a donor is the same for both a cross and direct match, but the confidence for each is different,
                // We should return the best confidence amongst orientations provided
                var selectedConfidences = confidences
                    .OrderByDescending(c => Math.Min((int) c.Item1, (int) c.Item2))
                    .ThenByDescending(c => (int) c.Item1 + (int) c.Item2)
                    .First();

                confidenceResults.SetAtLocus(locus, TypePositions.One, selectedConfidences.Item1);
                confidenceResults.SetAtLocus(locus, TypePositions.Two, selectedConfidences.Item2);
            });

            return confidenceResults;
        }

        private MatchConfidence CalculateConfidenceForOrientation(
            Locus locus,
            TypePositions position,
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
            TypePositions position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, position));
        }

        private MatchConfidence CalculateConfidenceForCrossMatch(
            Locus locus,
            TypePositions position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            switch (position)
            {
                case TypePositions.One:
                    return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.Two));
                case TypePositions.Two:
                    return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.One));
                case TypePositions.None:
                case TypePositions.Both:
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }
}