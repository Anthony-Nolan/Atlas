namespace Atlas.MatchingAlgorithm.Common.Models.Scoring
{
    public enum MatchOrientation
    {
        // Locus matches positions 1 vs 2, and 2 vs 1
        Cross,
        // Locus matches positions 1 vs 1, and 2 vs 2
        Direct
    }
}