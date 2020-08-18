namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    public enum MatchGrade
    {
        /// Mismatch grades
        Mismatch,
        PermissiveMismatch,

        // Grades for Serology-level matches
        Broad,
        Split,
        Associated,

        // Grades for Null vs. Null allele matches
        NullMismatch,
        NullPartial,
        NullCDna,
        NullGDna,

        // Grades for Expressing vs. Expressing allele matches
        PGroup,
        GGroup,
        Protein,
        CDna,
        GDna
    }
}