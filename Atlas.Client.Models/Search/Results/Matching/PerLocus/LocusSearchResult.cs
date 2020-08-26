namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    public class LocusSearchResult
    {
        /// <summary>
        ///     Determined by whether matching was requested for this locus or not.
        ///     e.g. In a 6/6 search, loci C and DQB1 will have their individual match counts populated via the scoring process,
        ///     and these match grades will not be included in the total Match Count in the api response
        /// </summary>
        public bool IsLocusMatchCountIncludedInTotal { get; set; }

        /// <summary>
        ///     The number of matches within this locus.
        ///     Either 0, 1 or 2 if the locus is typed.
        ///     If the locus is not typed this will be 2, since there is a potential match.
        ///     If matching and/or scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchCount { get; set; }

        /// <summary>
        ///     Reports whether this locus is typed for the given donor.
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public bool? IsLocusTyped { get; set; }

        /// <summary>
        ///     A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchGradeScore { get; set; }

        /// <summary>
        ///     A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchConfidenceScore { get; set; }

        /// <summary>
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public LocusPositionScoreDetails ScoreDetailsAtPositionOne { get; set; }

        /// <summary>
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public LocusPositionScoreDetails ScoreDetailsAtPositionTwo { get; set; }
    }
}