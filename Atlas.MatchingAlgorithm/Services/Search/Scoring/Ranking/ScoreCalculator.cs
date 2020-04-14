﻿using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories;

namespace Atlas.MatchingAlgorithm.Services.Scoring.Ranking
{
    public interface IMatchScoreCalculator
    {
        /// <summary>
        /// Converts a match grade to a numeric score, to allow for grade weighting
        /// </summary>
        int CalculateScoreForMatchGrade(MatchGrade matchGrade);
        
        
        /// <summary>
        /// Converts a match confidence to a numeric score, to allow for confidence weighting
        /// </summary>
        int CalculateScoreForMatchConfidence(MatchConfidence matchConfidence);
    }

    public class MatchScoreCalculator: IMatchScoreCalculator
    {
        private readonly IScoringWeightingRepository weightingRepository;

        public MatchScoreCalculator(IScoringWeightingRepository weightingRepository)
        {
            this.weightingRepository = weightingRepository;
        }
        
        public int CalculateScoreForMatchGrade(MatchGrade matchGrade)
        {
            return weightingRepository.GetGradeWeighting(matchGrade);
        }

        public int CalculateScoreForMatchConfidence(MatchConfidence matchConfidence)
        {
            return weightingRepository.GetConfidenceWeighting(matchConfidence);
        }
    }
}