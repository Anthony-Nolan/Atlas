using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class ExpandedHla
    {
        public string Name { get; set; }
        public Locus Locus { get; set; }
        public IEnumerable<string> PGroups { get; set; }
    }
}