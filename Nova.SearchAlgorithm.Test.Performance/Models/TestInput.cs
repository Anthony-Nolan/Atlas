using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Performance.Models
{
    public class TestInput
    {
        public AlgorithmInstanceInfo AlgorithmInstanceInfo { get; set; }
        
        public string DonorId { get; set; }
    
        public SearchType SearchType { get; set; }
        public DonorType DonorType { get; set; }
        public bool IsAlignedRegistriesSearch { get; set; }

        public PhenotypeInfo<string> Hla { get; set; }
        
        public int? SolarSearchElapsedMilliseconds { get; set; }
        public int? SolarSearchMatchedDonors { get; set; }
    }
}