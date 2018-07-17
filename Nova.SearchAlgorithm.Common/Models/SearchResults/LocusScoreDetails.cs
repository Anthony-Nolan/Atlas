namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusScoreDetails
    {
        /// <summary>
        /// A numeric value representing the relative match grade at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public int MatchGradeScore { get; set; }

        /// <summary>
        /// A numeric value representing the relative match confidence at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public int MatchConfidenceScore { get; set; }

        public LocusPositionScoreDetails ScoreDetails1;
        public LocusPositionScoreDetails ScoreDetails2;
    }
}