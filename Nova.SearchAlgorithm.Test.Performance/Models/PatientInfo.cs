using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Performance.Models
{
    public class PatientInfo
    {
        public string PatientId { get; set; }
        public PhenotypeInfo<string> Hla { get; set; }
    }
}