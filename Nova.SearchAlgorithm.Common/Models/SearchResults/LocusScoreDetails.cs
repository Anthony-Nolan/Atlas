namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusScoreDetails
    {
        /// <summary>
        /// A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchGradeScore => (int) ScoreDetailsAtPosition1.MatchGrade + (int) ScoreDetailsAtPosition2.MatchGrade;

        /// <summary>
        /// A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchConfidenceScore => (int) ScoreDetailsAtPosition2.MatchConfidence + (int) ScoreDetailsAtPosition2.MatchConfidence;

        public LocusPositionScoreDetails ScoreDetailsAtPosition1;
        public LocusPositionScoreDetails ScoreDetailsAtPosition2;
    }
}