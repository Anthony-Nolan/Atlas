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
        
        public int? SolarSearchElapsedMilliseconds { get; set; }
        public int? SolarSearchMatchedDonors { get; set; }
    }
}