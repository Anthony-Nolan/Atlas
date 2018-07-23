namespace Nova.SearchAlgorithm.Common.Models.Scoring
{
    /// <summary>
    /// Values for the grade of a given match. Ordered to allow for selecting the best grade - higher numbers being a better grade
    /// </summary>
    public enum MatchGrade
    {
        NotCalculated = 0,
        Mismatch = 1,
        Broad = 2,
        Split = 3,
        Associated = 4,
        PGroup = 5,
        GGroup = 6,
        Protein = 7,
        CDna = 8,
        GDna = 9
    }
}