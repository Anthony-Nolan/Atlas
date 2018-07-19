using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders.SearchResults
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
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    },
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    }
                },
                ScoreDetailsAtLocusB = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    },
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    }
                },
                ScoreDetailsAtLocusC = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    },
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    }
                },
                ScoreDetailsAtLocusDqb1 = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    },
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    }
                },
                ScoreDetailsAtLocusDrb1 = new LocusScoreDetails
                {
                    ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    },
                    ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                    {
                        MatchConfidenceScore = 0,
                        MatchGradeScore = 0,
                    }
                },
            };
        }

        public ScoreResultBuilder WithMatchGradeAtLocus(Locus locus, MatchGrade grade)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.ScoreDetailsAtPosition1.MatchGrade = grade;
            locusScoreDetails.ScoreDetailsAtPosition2.MatchGrade = grade;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }
        
        public ScoreResultBuilder WithMatchGradeScoreAtLocus(Locus locus, int matchGradeScore)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.ScoreDetailsAtPosition1.MatchGradeScore = matchGradeScore / 2;
            locusScoreDetails.ScoreDetailsAtPosition2.MatchGradeScore = matchGradeScore / 2 + matchGradeScore % 2;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }

        public ScoreResultBuilder WithMatchConfidenceAtLocus(Locus locus, MatchConfidence matchConfidence)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.ScoreDetailsAtPosition1.MatchConfidence = matchConfidence;
            locusScoreDetails.ScoreDetailsAtPosition2.MatchConfidence = matchConfidence;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }

        public ScoreResultBuilder WithMatchConfidenceScoreAtLocus(Locus locus, int matchConfidenceScore)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.ScoreDetailsAtPosition1.MatchConfidenceScore = matchConfidenceScore / 2;
            locusScoreDetails.ScoreDetailsAtPosition2.MatchConfidenceScore = matchConfidenceScore / 2 + matchConfidenceScore % 2;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }
        
        public ScoreResult Build()
        {
            return scoreResult;
        }
    }
}