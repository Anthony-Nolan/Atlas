using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Performance.Models
{
    public class TestInput
    {
        public AlgorithmInstanceInfo AlgorithmInstanceInfo { get; set; }
        
        public string PatientId { get; set; }
    
        public SearchType SearchType { get; set; }
        public DonorType DonorType { get; set; }
        public bool IsAlignedRegistriesSearch { get; set; }

        public PhenotypeInfo<string> Hla { get; set; }
        
        /// <summary>
        /// Solar timings were recorded by Sharon on the search team, running against SOLAR via the SOLAR application
        /// </summary>
        public int? SolarSearchElapsedMilliseconds { get; set; }
        /// <summary>
        /// Solar results were recorded by Sharon on the search team, running against SOLAR via the SOLAR application
        /// </summary>
        public int? SolarSearchMatchedDonors { get; set; }
    }
}