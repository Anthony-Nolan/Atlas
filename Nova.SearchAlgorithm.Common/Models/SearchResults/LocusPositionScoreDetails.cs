using Nova.SearchAlgorithm.Common.Models.Scoring;

namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusPositionScoreDetails
    {
        /// <summary>
        /// The match grade at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchGrade MatchGrade;
        
        /// <summary>
        /// The match confidence at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchConfidence MatchConfidence;
    }
}