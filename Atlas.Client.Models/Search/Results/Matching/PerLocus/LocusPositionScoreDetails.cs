namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    public class LocusPositionScoreDetails
    {
        /// <summary>
        ///     The match confidence at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchConfidence MatchConfidence;

        /// <summary>
        ///     A numeric score given to the confidence by the ranking service, to allow for weighting by confidence
        /// </summary>
        public int? MatchConfidenceScore;

        /// <summary>
        ///     The match grade at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchGrade MatchGrade;

        /// <summary>
        ///     A numeric score given to the grade by the ranking service, to allow for weighting by grade
        /// </summary>
        public int? MatchGradeScore;
    }
}