namespace Atlas.MatchingAlgorithm.Test.Performance.Models
{
    public class AlgorithmInstanceInfo
    {
        public Environment Environment { get; set; } 
        
        public string BaseUrl { get; set; }
        public string Apikey { get; set; }
        
        public string DatabaseSize { get; set; } = "N/A";
        public string DonorDataSet { get; set; } = "Full UAT database";
    }
}