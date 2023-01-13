using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Performance.Models
{
    public class PatientInfo
    {
        public string PatientId { get; set; }
        public PhenotypeInfo<string> Hla { get; set; }
    }
}