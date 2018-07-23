namespace Nova.SearchAlgorithm.Common.Models.Scoring
{
    /// <summary>
    /// Values for the grade of a given match. Ordered to allow for selecting the best grade - higher numbers being a better grade
    /// </summary>
    public enum MatchGrade
    {
        GDna = 8,
        CDna = 7,
        Protein = 6,
        GGroup = 5,
        PGroup = 4,
        Associated = 3,
        Split = 2,
        Broad = 1,
        Mismatch = 0
    }
}