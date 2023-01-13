using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Test.Performance.Models
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