﻿using System;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults
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

        public LocusScoreDetailsBuilder WithMatchGradeScoreAtPosition(LocusPosition position, int? matchGradeScore)
        {
            var scoreDetails = GetScoreDetailsAtPosition(position);
            scoreDetails.MatchGradeScore = matchGradeScore;
            SetScoreDetailsAtPosition(position, scoreDetails);

            return this;
        }

        public LocusScoreDetailsBuilder WithMatchConfidenceScoreAtPosition(LocusPosition position, int? matchConfidenceScore)
        {
            var scoreDetails = GetScoreDetailsAtPosition(position);
            scoreDetails.MatchConfidenceScore = matchConfidenceScore;
            SetScoreDetailsAtPosition(position, scoreDetails);

            return this;
        }

        public LocusScoreDetailsBuilder WithMatchConfidenceAtPosition(LocusPosition position, MatchConfidence matchConfidence)
        {
            var scoreDetails = GetScoreDetailsAtPosition(position);
            scoreDetails.MatchConfidence = matchConfidence;
            SetScoreDetailsAtPosition(position, scoreDetails);

            return this;
        }

        public LocusScoreDetails Build()
        {
            return locusScore;
        }

        private LocusPositionScoreDetails GetScoreDetailsAtPosition(LocusPosition position)
        {
            switch (position)
            {
                case LocusPosition.One:
                    return locusScore.ScoreDetailsAtPosition1;
                case LocusPosition.Two:
                    return locusScore.ScoreDetailsAtPosition2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        private void SetScoreDetailsAtPosition(LocusPosition position, LocusPositionScoreDetails locusPositionScoreDetails)
        {
            switch (position)
            {
                case LocusPosition.One:
                    locusScore.ScoreDetailsAtPosition1 = locusPositionScoreDetails;
                    break;
                case LocusPosition.Two:
                    locusScore.ScoreDetailsAtPosition2 = locusPositionScoreDetails;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }
}