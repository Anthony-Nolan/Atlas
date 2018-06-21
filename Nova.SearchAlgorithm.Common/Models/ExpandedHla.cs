using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class ExpandedHla
    {
        public string OriginalName { get; set; }
        public string LookupName { get; set; }
        public Locus Locus { get; set; }
        public IEnumerable<string> PGroups { get; set; }
    }
}