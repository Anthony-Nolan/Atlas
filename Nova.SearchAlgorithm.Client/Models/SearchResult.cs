using System;

namespace Nova.SearchAlgorithm.Client.Models
{
    public enum MatchGrades
    {
        Unknown,
        ExactMatch,
        PotentialMatch,
        Mismatch,
        MinorMismatch
    }

    public class SearchResult
    {
        public int SearchRequestId { get; set; }
        public MatchGrades MatchGrade { get; set; }
        public string MatchType { get; set; }
        public DateTime? SearchRunDate { get; set; }
        public int OrderNumber { get; set; }
        public int TotalScore { get; set; }
        public Donor Donor { get; set; } = new Donor();
    }
}