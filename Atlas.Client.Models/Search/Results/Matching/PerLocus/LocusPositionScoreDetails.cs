namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    public class LocusPositionScoreDetails
    {
        /// <summary>
        /// The match confidence at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchConfidence MatchConfidence;

        /// <summary>
        /// A numeric score given to the confidence by the ranking service, to allow for weighting by confidence
        /// </summary>
        public int? MatchConfidenceScore;

        /// <summary>
        /// The match grade at this locus, according to the scoring algorithm, for validation and debugging.
        /// </summary>
        public MatchGrade MatchGrade;

        /// <summary>
        /// A numeric score given to the grade by the ranking service, to allow for weighting by grade
        /// </summary>
        public int? MatchGradeScore;

        /// <summary>
        /// Will be `true`, except for:
        ///     - If <see cref="MatchGrade"/> is `Mismatch` AND the mismatch is also antigen mismatched, then it will be `false`
        ///     - If <see cref="MatchGrade"/> is `Unknown`, then it will be `null`
        ///     - If <see cref="MatchGrade"/> is any of the Null vs. Null allele grades, then it will be `false`
        /// </summary>
        public bool? IsAntigenMatch;
    }
}