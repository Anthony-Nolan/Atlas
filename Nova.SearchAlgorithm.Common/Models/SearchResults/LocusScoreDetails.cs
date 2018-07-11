namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusScoreDetails
    {
        /// <summary>
        /// The match grade at this locus, according to the scoring algorithm,
        /// for validation and debugging.
        /// </summary>
        public int MatchGrade { get; set; }

        /// <summary>
        /// The match confidence at this locus, according to the scoring algorithm,
        /// for validation and debugging.
        /// </summary>
        public int MatchConfidence { get; set; }
    }
}