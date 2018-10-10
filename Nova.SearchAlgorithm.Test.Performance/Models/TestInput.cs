using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Performance.Models
{
    public class TestInput
    {
        public TestInput()
        {
        }

        public TestInput(PatientInfo patientInfo)
        {
            PatientId = patientInfo.PatientId;
            Hla = patientInfo.Hla;
        }

        public AlgorithmInstanceInfo AlgorithmInstanceInfo { get; set; } = TestCases.LocalAlgorithmInstanceInfo;
        
        public string PatientId { get; set; }
    
        public SearchType SearchType { get; set; }
        public DonorType DonorType { get; set; } = DonorType.Adult;
        public bool IsAlignedRegistriesSearch { get; set; } = true;

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