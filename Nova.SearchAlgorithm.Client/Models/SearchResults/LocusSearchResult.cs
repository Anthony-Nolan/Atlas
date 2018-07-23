using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    public class LocusSearchResult
    {
        /// <summary>
        /// Reports whether this locus is typed for the given donor.
        /// </summary>
        public bool IsLocusTyped { get; set; }

        /// <summary>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is a potential match.
        /// </summary>
        public int MatchCount { get; set; }
        
        /// <summary>
        /// A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchGradeScore { get; set; }

        /// <summary>
        /// A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchConfidenceScore { get; set; }

        public LocusPositionScoreDetails ScoreDetailsAtPositionOne { get; set; }
        public LocusPositionScoreDetails ScoreDetailsAtPositionTwo { get; set; }
    }
}