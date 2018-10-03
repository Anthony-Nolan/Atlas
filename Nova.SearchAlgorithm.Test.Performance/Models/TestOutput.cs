using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Test.Performance.Models
{
    public class TestOutput
    {
        public string DonorId { get; set; }
    
        public SearchType SearchType { get; set; }
        public DonorType DonorType { get; set; }
        
        public long ElapsedMilliseconds { get; set; }
        public int MatchedDonors { get; set; }
        public bool IsAlignedRegistriesSearch { get; set; }
        
        public string HlaA1 { get; set; }
        public string HlaA2 { get; set; }
        public string HlaB1 { get; set; }
        public string HlaB2 { get; set; }
        public string HlaC1 { get; set; }
        public string HlaC2 { get; set; }
        public string HlaDqb11 { get; set; }
        public string HlaDqb12 { get; set; }
        public string HlaDrb11 { get; set; }
        public string HlaDrb12 { get; set; }
    }
}