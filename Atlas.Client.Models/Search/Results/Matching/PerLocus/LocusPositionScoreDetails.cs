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
        /// Will be `true` if patient and donor overlap in their matching serologies.
        /// Will be `null` if either patient or donor is a typing that does not have any serologies assigned within IMGT/HLA `rel_dna_ser`, e.g.,  non-expressing alleles, alleles with only "?" assignment, etc.
        /// </summary>
        public bool? IsAntigenMatch;
    }
}