using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class ExpandedHla
    {
        public string Name { get; set; }
        public Locus Locus { get; set; }
        public IEnumerable<string> PGroups { get; set; }
    }
}