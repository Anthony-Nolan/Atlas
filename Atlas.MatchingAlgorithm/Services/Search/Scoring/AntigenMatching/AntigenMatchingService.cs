using System.Linq;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching
{
    public interface IAntigenMatchingService : IPositionalScorerBase<bool?>
    {
    }

    internal class AntigenMatchingService : PositionalScorerBase<bool?>, IAntigenMatchingService
    {
        private static readonly PositionalScorerSettings<bool?> Settings = new() {
            DefaultLocusScore = null,
            DefaultDpb1Score = null,
            HandleNonExpressingAlleles = true
        };

        private readonly IAntigenMatchCalculator calculator;
        private readonly IScoringCache scoringCache;

        public AntigenMatchingService(
            IHlaCategorisationService hlaCategorisationService,
            IAntigenMatchCalculator calculator, 
            IScoringCache scoringCache) : base(hlaCategorisationService, Settings)
        {
            this.calculator = calculator;
            this.scoringCache = scoringCache;
        }

        protected override bool? ScorePosition(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddIsAntigenMatch(
                patientMetadata?.Locus,
                patientMetadata?.LookupName,
                donorMetadata?.LookupName,
                c => calculator.IsAntigenMatch(patientMetadata, donorMetadata));
        }

        protected override int SumLocusScore(LocusInfo<bool?> score)
        {
            return new[] { score.Position1, score.Position2 }.Count(r => r.HasValue && r.Value);
        }
    }
}