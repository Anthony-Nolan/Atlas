namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    /// <summary>
    /// Values for the grade of a given match. Ordered to allow for selecting the best grade - higher numbers being a better grade
    /// </summary>
    public enum MatchGrade
    {
        GDna = 7,
        CDna = 6,
        Protein = 5,
        GGroup = 4,
        PGroup = 3,
        Associated = 2,
        Split = 1,
        Mismatch = 0
    }
}