namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    /// <summary>
    /// Values for the grade of a given match. 
    /// Ordered to allow for selecting the best grade: higher numbers being a better grade.
    /// </summary>
    public enum MatchGrade
    {
        /// <summary>
        /// Mismatch values significantly lower than match values to ensure 
        /// the demotion of single match-mismatch results below double-match results.
        /// </summary>
        Mismatch = 0,
        PermissiveMismatch = 1,

        // Grades for Serology-level matches
        Broad = 11,
        Split = 12,
        Associated = 13,

        // Grades for Null vs. Null allele matches
        NullMismatch = 14,
        NullPartial = 15,
        NullCDna = 16,
        NullGDna = 17,

        // Grades for Expressing vs. Expressing allele matches
        PGroup = 18,
        GGroup = 19,
        Protein = 20,
        CDna = 21,
        GDna = 22
    }
}