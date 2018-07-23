using System;

namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusScoreDetails
    {
        /// <summary>
        /// A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchGradeScore
        {
            get
            {
                if (ScoreDetailsAtPosition1.MatchGradeScore == null || ScoreDetailsAtPosition2.MatchGradeScore == null)
                {
                    throw new Exception("Cannot access grade score before it has been assigned - ensure that ranking has been performed on this result");
                }
                return (int) ScoreDetailsAtPosition1.MatchGradeScore + (int) ScoreDetailsAtPosition2.MatchGradeScore;
            }
        }

        /// <summary>
        /// A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchConfidenceScore
        {
            get
            {
                if (ScoreDetailsAtPosition1?.MatchConfidenceScore == null || ScoreDetailsAtPosition2?.MatchConfidenceScore == null)
                {
                    throw new Exception("Cannot access confidence score before it has been assigned - ensure that ranking has been performed on this result");
                }
                return (int) ScoreDetailsAtPosition1.MatchConfidenceScore + (int) ScoreDetailsAtPosition2.MatchConfidenceScore;
            }
        }

        public LocusPositionScoreDetails ScoreDetailsAtPosition1;
        public LocusPositionScoreDetails ScoreDetailsAtPosition2;
    }
}