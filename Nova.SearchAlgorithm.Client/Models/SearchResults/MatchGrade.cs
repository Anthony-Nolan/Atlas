namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    /// <summary>
    /// Values for the grade of a given match. 
    /// Ordered to allow for selecting the best grade: higher numbers being a better grade.
    /// </summary>
    public enum MatchGrade
    {
        /// <summary>
        /// Mismatch value significantly lower than match values to ensure 
        /// the demotion of single match-mismatch results below double-match results.
        /// </summary>
        Mismatch = 0,

        Broad = 11,
        Split = 12,
        Associated = 13,
        PGroup = 14,
        GGroup = 15,
        Protein = 16,
        CDna = 17,
        GDna = 18
    }
}