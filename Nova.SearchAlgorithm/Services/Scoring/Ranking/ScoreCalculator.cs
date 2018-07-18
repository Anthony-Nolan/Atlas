using Nova.SearchAlgorithm.Common.Models.Scoring;

namespace Nova.SearchAlgorithm.Services.Scoring.Ranking
{
    public interface IMatchGradeScoreCalculator
    {
        /// <summary>
        /// Converts a match grade to a numeric score, to allow for grade weighting
        /// </summary>
        int CalculateScoreForMatchGrade(MatchGrade matchGrade);
        
        
        /// <summary>
        /// Converts a match confidence to a numeric score, to allow for confidence weighting
        /// </summary>
        int CalculateScoreForMatchConfidence(MatchGrade matchConfidence);
    }

    public class MatchGradeScoreCalculator: IMatchGradeScoreCalculator
    {
        // TODO: NOVA-1467: Apply appropriate weighting to match grade scores
        public int CalculateScoreForMatchGrade(MatchGrade matchGrade)
        {
            return (int) matchGrade;
        }

        // TODO: NOVA-1467: Apply appropriate weighting to match confidence scores
        public int CalculateScoreForMatchConfidence(MatchGrade matchConfidence)
        {
            return (int) matchConfidence;
        }
    }
}