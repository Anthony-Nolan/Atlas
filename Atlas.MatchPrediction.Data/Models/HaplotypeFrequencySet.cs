using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }

        [MaxLength(150)]
        public string RegistryCode { get; set; }

        [MaxLength(150)]
        public string EthnicityCode { get; set; }

        public bool Active { get; set; }
    }
}