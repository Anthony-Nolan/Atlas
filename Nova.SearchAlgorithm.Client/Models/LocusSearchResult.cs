namespace Nova.SearchAlgorithm.Client.Models
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