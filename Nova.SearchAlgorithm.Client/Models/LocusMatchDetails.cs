namespace Nova.SearchAlgorithm.Client.Models
{
    public class LocusMatchDetails
    {
        /// <summary>
        /// Reports whether this locus is typed for the given donor.
        /// </summary>
        public bool IsLocusTyped { get; set; }

        /// <summary>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// Null if the locus is not typed.
        /// </summary>
        public int? MatchCount { get; set; }

        /// <summary>
        /// The match gradeat this locus,  according to the scoring algorithm,
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