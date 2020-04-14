using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.Performance.Models
{
    public class PatientInfo
    {
        public string PatientId { get; set; }
        public PhenotypeInfo<string> Hla { get; set; }
    }
}