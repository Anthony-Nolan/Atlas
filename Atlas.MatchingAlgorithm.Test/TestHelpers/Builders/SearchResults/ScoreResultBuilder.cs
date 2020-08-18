using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults
{
    public class ScoreResultBuilder
    {
        private readonly ScoreResult scoreResult;

        public ScoreResultBuilder()
        {
            scoreResult = new ScoreResult
            {
                AggregateScoreDetails = new AggregateScoreDetails(),
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
                ScoreDetailsAtLocusDpb1 = new LocusScoreDetails
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
        
        public ScoreResultBuilder WithMatchGradeAtLocusPosition(Locus locus, LocusPosition position, MatchGrade matchGrade)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            switch (position)
            {
                case LocusPosition.One:
                    locusScoreDetails.ScoreDetailsAtPosition1.MatchGrade = matchGrade;
                    break;
                case LocusPosition.Two:
                    locusScoreDetails.ScoreDetailsAtPosition2.MatchGrade = matchGrade;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
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

        public ScoreResultBuilder WithMatchConfidenceAtAllLoci(MatchConfidence matchConfidence)
        {
            return this
                .WithMatchConfidenceAtLocus(Locus.A, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.B, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, matchConfidence);
        }

        public ScoreResultBuilder WithMatchGradeAtAllLoci(MatchGrade matchGrade)
        {
            return this
                .WithMatchGradeAtLocus(Locus.A, matchGrade)
                .WithMatchGradeAtLocus(Locus.B, matchGrade)
                .WithMatchGradeAtLocus(Locus.C, matchGrade)
                .WithMatchGradeAtLocus(Locus.Dpb1, matchGrade)
                .WithMatchGradeAtLocus(Locus.Dqb1, matchGrade)
                .WithMatchGradeAtLocus(Locus.Drb1, matchGrade);
        }
        
        public ScoreResultBuilder WithMatchConfidenceAtLocus(Locus locus, MatchConfidence matchConfidence)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.ScoreDetailsAtPosition1.MatchConfidence = matchConfidence;
            locusScoreDetails.ScoreDetailsAtPosition2.MatchConfidence = matchConfidence;
            scoreResult.SetScoreDetailsForLocus(locus, locusScoreDetails);
            return this;
        }
        
        public ScoreResultBuilder WithMatchConfidenceAtLocusPosition(Locus locus, LocusPosition position, MatchConfidence matchConfidence)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            switch (position)
            {
                case LocusPosition.One:
                    locusScoreDetails.ScoreDetailsAtPosition1.MatchConfidence = matchConfidence;
                    break;
                case LocusPosition.Two:
                    locusScoreDetails.ScoreDetailsAtPosition2.MatchConfidence = matchConfidence;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
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

        /// <summary>
        /// As match count is a product of the confidences, this method implicitly sets the
        /// match count by setting an appropriate number of confidences to "Mismatch". 
        /// </summary>
        public ScoreResultBuilder WithMatchCountAtLocus(Locus locus, int matchCount)
        {
            switch (matchCount)
            {
                case 2:
                    return this.WithMatchConfidenceAtLocus(locus, MatchConfidence.Definite);
                case 1:
                    return this.WithMatchConfidenceAtLocusPosition(locus, LocusPosition.One, MatchConfidence.Mismatch);
                case 0:
                    return this.WithMatchConfidenceAtLocus(locus, MatchConfidence.Mismatch);
                default:
                    throw new ArgumentException();
            }
        }

        public ScoreResultBuilder WithTypingAtLocus(Locus locus, bool isTyped = true)
        {
            var locusScoreDetails = scoreResult.ScoreDetailsForLocus(locus);
            locusScoreDetails.IsLocusTyped = isTyped;
            return this;
        }
        
        public ScoreResultBuilder WithAggregateScoringData(AggregateScoreDetails aggregateScoreDetails)
        {
            scoreResult.AggregateScoreDetails = aggregateScoreDetails;
            return this;
        }

        public ScoreResult Build()
        {
            return scoreResult;
        }
    }
}