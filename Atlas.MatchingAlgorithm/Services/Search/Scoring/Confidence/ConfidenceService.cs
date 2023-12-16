using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence
{
    public interface IConfidenceService : IPositionalScorerBase<MatchConfidence>
    {
    }

    public class ConfidenceService : PositionalScorerBase<MatchConfidence>, IConfidenceService
    {
        private readonly IConfidenceCalculator confidenceCalculator;
        private readonly IScoringCache scoringCache;

        public ConfidenceService(
            IHlaCategorisationService hlaCategorisationService, 
            IConfidenceCalculator confidenceCalculator, 
            IScoringCache scoringCache) : base(hlaCategorisationService, MatchConfidence.Potential)
        {
            this.confidenceCalculator = confidenceCalculator;
            this.scoringCache = scoringCache;
        }

        /// <inheritdoc />
        protected override MatchConfidence ScorePosition(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddMatchConfidence(
                patientMetadata?.Locus,
                patientMetadata?.LookupName,
                donorMetadata?.LookupName,
                c => confidenceCalculator.CalculateConfidence(patientMetadata, donorMetadata));
        }

        /// <inheritdoc />
        protected override int SumLocusScore(LocusInfo<MatchConfidence> score)
        {
            return (int)score.Position1 + (int)score.Position2;
        }
    }
}