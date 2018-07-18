using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class ScoreResultBuilder
    {
        private readonly ScoreResult scoreResult;

        public ScoreResultBuilder()
        {
            scoreResult = new ScoreResult
            {
                ScoreDetailsAtLocusA = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails(),
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails()
                },
                ScoreDetailsAtLocusB = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails(),
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails()
                },
                ScoreDetailsAtLocusC = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails(),
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails()
                },
                ScoreDetailsAtLocusDqb1 = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails(),
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails()
                },
                ScoreDetailsAtLocusDrb1 = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails(),
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails()
                },
            };
        }

        public ScoreResultBuilder WithMatchGradeAtLocus(Locus locus, MatchGrade grade)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus) ?? new LocusScoreDetails();
            var scoreDetails1 = locusScoreDetails.ScoreDetailsAtPosition1 ?? new LocusPositionScoreDetails();
            var scoreDetails2 = locusScoreDetails.ScoreDetailsAtPosition2 ?? new LocusPositionScoreDetails();
            scoreDetails1.MatchGrade = grade;
            scoreDetails2.MatchGrade = grade;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }

        public ScoreResult Build()
        {
            return scoreResult;
        }
    }
}