using System;
using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string RegistryCode { get; set; }

        [MaxLength(256)]
        public string EthnicityCode { get; set; }

        public bool Active { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DateTimeAdded { get; set; }
    }
}