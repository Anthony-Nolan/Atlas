namespace Nova.SearchAlgorithm.Common.Models
{
    public class PotentialSearchResult
    {
        public DonorResult Donor { get; set; }
        public int TotalMatchCount { get; set; }
        public int TypedLociCount { get; set; }
        public int MatchRank { get; set; }
        public int TotalMatchGrade { get; set; }
        public int TotalMatchConfidence { get; set; }
        public LocusMatchDetails MatchDetailsAtLocusA { get; set; }
        public LocusMatchDetails MatchDetailsAtLocusB { get; set; }
        public LocusMatchDetails MatchDetailsAtLocusC { get; set; }
        public LocusMatchDetails MatchDetailsAtLocusDrb1 { get; set; }
        public LocusMatchDetails MatchDetailsAtLocusDqb1 { get; set; }
    }
}
