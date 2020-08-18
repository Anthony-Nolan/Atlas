using System;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
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
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
            PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            var confidenceResults = new PhenotypeInfo<MatchConfidence>();

            patientMetadata.EachLocus((locus, patientMetadataAtLocus) =>
            {
                var matchGradesAtLocus = matchGrades.GetLocus(locus);
                var orientations = matchGradesAtLocus.Position1.Orientations;

                var confidences = orientations.Select(o => new LocusInfo<MatchConfidence>(
                    CalculateConfidenceForOrientation(locus, LocusPosition.One, patientMetadataAtLocus.Position1, donorMetadata, o),
                    CalculateConfidenceForOrientation(locus, LocusPosition.Two, patientMetadataAtLocus.Position2, donorMetadata, o)
                ));

                // In the case where the best grade for a donor is the same for both a cross and direct match, but the confidence for each is different,
                // We should return the best confidence amongst orientations provided
                var selectedConfidences = confidences
                    .OrderByDescending(c => (int) c.Position1 + (int) c.Position2)
                    .First();

                confidenceResults = confidenceResults
                    .SetPosition(locus, LocusPosition.One, selectedConfidences.Position1)
                    .SetPosition(locus, LocusPosition.Two, selectedConfidences.Position2);
            });

            return confidenceResults;
        }

        private MatchConfidence CalculateConfidenceForOrientation(
            Locus locus,
            LocusPosition position,
            IHlaScoringMetadata patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
            MatchOrientation matchOrientation)
        {
            return matchOrientation switch
            {
                MatchOrientation.Direct => CalculateConfidenceForDirectMatch(locus, position, patientMetadata, donorMetadata),
                MatchOrientation.Cross => CalculateConfidenceForCrossMatch(locus, position, patientMetadata, donorMetadata),
                _ => throw new ArgumentOutOfRangeException(nameof(matchOrientation), matchOrientation, null)
            };
        }

        private MatchConfidence CalculateConfidenceForDirectMatch(
            Locus locus,
            LocusPosition position,
            IHlaScoringMetadata patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            return GetConfidence(patientMetadata, donorMetadata.GetPosition(locus, position));
        }

        private MatchConfidence CalculateConfidenceForCrossMatch(
            Locus locus,
            LocusPosition position,
            IHlaScoringMetadata patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            return position switch
            {
                LocusPosition.One => GetConfidence(patientMetadata, donorMetadata.GetPosition(locus, LocusPosition.Two)),
                LocusPosition.Two => GetConfidence(patientMetadata, donorMetadata.GetPosition(locus, LocusPosition.One)),
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
        }

        private MatchConfidence GetConfidence(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddMatchConfidence(
                patientMetadata?.Locus,
                patientMetadata?.LookupName,
                donorMetadata?.LookupName,
                c => confidenceCalculator.CalculateConfidence(patientMetadata, donorMetadata));
        }
    }
}