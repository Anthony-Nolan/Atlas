using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    public interface IGradingService : IPositionalScorerBase<MatchGrade>
    {
    }

    public class GradingService : PositionalScorerBase<MatchGrade>, IGradingService
    {
        private static readonly PositionalScorerSettings<MatchGrade> Settings = new() {
            DefaultLocusScore = MatchGrade.PGroup,
            DefaultDpb1Score = MatchGrade.Unknown,
            HandleNonExpressingAlleles = false
        };

        private readonly IScoringCache scoringCache;

        public GradingService(
            IHlaCategorisationService hlaCategorisationService,
            IScoringCache scoringCache) : base(hlaCategorisationService, Settings)
        {
            this.scoringCache = scoringCache;
        }

        protected override MatchGrade ScorePosition(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddMatchGrade(patientMetadata.Locus, patientMetadata.LookupName, donorMetadata.LookupName,
                c =>
                {
                    var calculator = GradingCalculatorFactory.GetGradingCalculator(
                        patientMetadata.HlaScoringInfo,
                        donorMetadata.HlaScoringInfo);
                    var grade = calculator.CalculateGrade(patientMetadata, donorMetadata);
                    return grade;
                });
        }

        protected override int SumLocusScore(LocusInfo<MatchGrade> score)
        {
            return (int)score.Position1 + (int)score.Position2;
        }
    }
}