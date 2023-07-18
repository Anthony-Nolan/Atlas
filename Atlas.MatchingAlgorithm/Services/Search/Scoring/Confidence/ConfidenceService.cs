using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
            LociInfo<List<MatchOrientation>> orientations);
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
            LociInfo<List<MatchOrientation>> orientations)
        {
            var confidenceResults = new PhenotypeInfo<MatchConfidence>();

            donorMetadata.EachLocus((locus, donorMetadataAtLocus) =>
            {
                var orientationsAtLocus = orientations.GetLocus(locus);

                var confidences = orientationsAtLocus.Select(o => new LocusInfo<MatchConfidence>(
                    CalculateConfidenceForOrientation(locus, LocusPosition.One, patientMetadata, donorMetadataAtLocus.Position1, o),
                    CalculateConfidenceForOrientation(locus, LocusPosition.Two, patientMetadata, donorMetadataAtLocus.Position2, o)
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
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            IHlaScoringMetadata donorMetadata,
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
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            IHlaScoringMetadata donorMetadata)
        {
            return GetConfidence(patientMetadata.GetPosition(locus, position), donorMetadata);
        }

        private MatchConfidence CalculateConfidenceForCrossMatch(
            Locus locus,
            LocusPosition position,
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            IHlaScoringMetadata donorMetadata)
        {
            return position switch
            {
                LocusPosition.One => GetConfidence(patientMetadata.GetPosition(locus, LocusPosition.Two), donorMetadata),
                LocusPosition.Two => GetConfidence(patientMetadata.GetPosition(locus, LocusPosition.One), donorMetadata),
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