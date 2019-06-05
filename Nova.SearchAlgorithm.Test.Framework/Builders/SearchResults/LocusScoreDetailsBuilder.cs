using System;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders.SearchResults
{
    public class LocusScoreDetailsBuilder
    {
        private readonly LocusScoreDetails locusScore;

        public LocusScoreDetailsBuilder()
        {
            locusScore = new LocusScoreDetails
            {
                IsLocusTyped = true,
                ScoreDetailsAtPosition1 = new LocusPositionScoreDetails
                {
                    MatchGrade = MatchGrade.Mismatch,
                    MatchConfidence = MatchConfidence.Mismatch,
                    MatchConfidenceScore = 0,
                    MatchGradeScore = 0
                },
                ScoreDetailsAtPosition2 = new LocusPositionScoreDetails
                {
                    MatchGrade = MatchGrade.Mismatch,
                    MatchConfidence = MatchConfidence.Mismatch,
                    MatchConfidenceScore = 0,
                    MatchGradeScore = 0
                }
            };
        }

        public LocusScoreDetailsBuilder WithMatchGradeScoreAtPosition(TypePosition typePosition, int? matchGradeScore)
        {
            var scoreDetails = GetScoreDetailsAtPosition(typePosition);
            scoreDetails.MatchGradeScore = matchGradeScore;
            SetScoreDetailsAtPosition(typePosition, scoreDetails);

            return this;
        }

        public LocusScoreDetailsBuilder WithMatchConfidenceScoreAtPosition(TypePosition typePosition, int? matchConfidenceScore)
        {
            var scoreDetails = GetScoreDetailsAtPosition(typePosition);
            scoreDetails.MatchConfidenceScore = matchConfidenceScore;
            SetScoreDetailsAtPosition(typePosition, scoreDetails);

            return this;
        }

        public LocusScoreDetailsBuilder WithMatchGradeAtPosition(TypePosition typePosition, MatchGrade matchGrade)
        {
            var scoreDetails = GetScoreDetailsAtPosition(typePosition);
            scoreDetails.MatchGrade = matchGrade;
            SetScoreDetailsAtPosition(typePosition, scoreDetails);

            return this;
        }

        public LocusScoreDetailsBuilder WithMatchConfidenceAtPosition(TypePosition typePosition, MatchConfidence matchConfidence)
        {
            var scoreDetails = GetScoreDetailsAtPosition(typePosition);
            scoreDetails.MatchConfidence = matchConfidence;
            SetScoreDetailsAtPosition(typePosition, scoreDetails);

            return this;
        }

        public LocusScoreDetails Build()
        {
            return locusScore;
        }

        private LocusPositionScoreDetails GetScoreDetailsAtPosition(TypePosition typePosition)
        {
            switch (typePosition)
            {
                case TypePosition.One:
                    return locusScore.ScoreDetailsAtPosition1;
                case TypePosition.Two:
                    return locusScore.ScoreDetailsAtPosition2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null);
            }
        }

        private void SetScoreDetailsAtPosition(TypePosition typePosition, LocusPositionScoreDetails locusPositionScoreDetails)
        {
            switch (typePosition)
            {
                case TypePosition.One:
                    locusScore.ScoreDetailsAtPosition1 = locusPositionScoreDetails;
                    break;
                case TypePosition.Two:
                    locusScore.ScoreDetailsAtPosition2 = locusPositionScoreDetails;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null);
            }
        }
    }
}