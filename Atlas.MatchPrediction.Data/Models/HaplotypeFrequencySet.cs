using System;

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }
        public string Registry { get; set; }
        public string Ethnicity { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DateTimeAdded { get; set; }
    }
}
