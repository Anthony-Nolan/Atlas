namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class LocusMatchDetails
    {
        /// <summary>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is a potential match.
        /// </summary>
        public int MatchCount { get; set; }
    }
}