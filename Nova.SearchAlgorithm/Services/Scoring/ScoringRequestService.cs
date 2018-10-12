using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Extensions;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IScoringRequestService
    {
        Task<ScoringResult> Score(ScoringRequest scoringRequest);
    }
    
    public class ScoringRequestService: IScoringRequestService
    {
        private readonly IDonorScoringService donorScoringService;

        public ScoringRequestService(IDonorScoringService donorScoringService)
        {
            this.donorScoringService = donorScoringService;
        }   
        
        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            var donorHla = scoringRequest.DonorHla.ToPhenotypeInfo();
            var patientHla = scoringRequest.PatientHla.ToPhenotypeInfo();

            var scoringResult = await donorScoringService.ScoreDonorHlaAgainstPatientHla(donorHla, patientHla);
            
            return new ScoringResult()
            {
                OverallMatchConfidence = scoringResult.OverallMatchConfidence,
                ConfidenceScore = scoringResult.ConfidenceScore,
                GradeScore = scoringResult.GradeScore,
                TotalMatchCount = scoringResult.MatchCount,
                PotentialMatchCount = scoringResult.PotentialMatchCount,
                SearchResultAtLocusA = MapSearchResultToApiLocusSearchResult(scoringResult, Locus.A),
                SearchResultAtLocusB = MapSearchResultToApiLocusSearchResult(scoringResult, Locus.B),
                SearchResultAtLocusC = MapSearchResultToApiLocusSearchResult(scoringResult, Locus.C),
                SearchResultAtLocusDqb1 = MapSearchResultToApiLocusSearchResult(scoringResult, Locus.Dqb1),
                SearchResultAtLocusDrb1 = MapSearchResultToApiLocusSearchResult(scoringResult, Locus.Drb1),
            };
        }

        private static LocusSearchResult MapSearchResultToApiLocusSearchResult(ScoreResult scoringResult, Locus locus)
        {
            var scoreDetailsForLocus = scoringResult.ScoreDetailsForLocus(locus);

            return new LocusSearchResult
            {
                IsLocusMatchCountIncludedInTotal = true,
                IsLocusTyped = scoreDetailsForLocus.IsLocusTyped,
                MatchCount = scoreDetailsForLocus.MatchCount(),
                MatchGradeScore = scoreDetailsForLocus.MatchGradeScore,
                MatchConfidenceScore = scoreDetailsForLocus.MatchConfidenceScore,
                ScoreDetailsAtPositionOne = new LocusPositionScoreDetails
                {
                    MatchConfidence = scoreDetailsForLocus.ScoreDetailsAtPosition1.MatchConfidence,
                    MatchConfidenceScore = scoreDetailsForLocus.ScoreDetailsAtPosition1.MatchConfidenceScore,
                    MatchGrade = scoreDetailsForLocus.ScoreDetailsAtPosition1.MatchGrade,
                    MatchGradeScore = scoreDetailsForLocus.ScoreDetailsAtPosition1.MatchGradeScore,
                },
                ScoreDetailsAtPositionTwo = new LocusPositionScoreDetails
                {
                    MatchConfidence = scoreDetailsForLocus.ScoreDetailsAtPosition2.MatchConfidence,
                    MatchConfidenceScore = scoreDetailsForLocus.ScoreDetailsAtPosition2.MatchConfidenceScore,
                    MatchGrade = scoreDetailsForLocus.ScoreDetailsAtPosition2.MatchGrade,
                    MatchGradeScore = scoreDetailsForLocus.ScoreDetailsAtPosition2.MatchGradeScore,
                }
            };
        }
    }
}