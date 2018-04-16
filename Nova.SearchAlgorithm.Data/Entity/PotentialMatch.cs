using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class PotentialMatch
    {
        public string Id { get; set; }
        public string MatchGrade { get; set; }
        public string MatchType { get; set; }
    }
}