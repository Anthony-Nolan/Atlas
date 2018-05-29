using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class ExpandedHla
    {
        public string Name { get; set; }
        public Locus Locus { get; set; }
        public IEnumerable<string> PGroups { get; set; }
    }
}