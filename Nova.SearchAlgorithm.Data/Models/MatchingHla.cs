using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Data.Models
{
    // TODO: Rename to ExpandedHla
    public class MatchingHla
    {
        public string Name { get; set; }
        public string Locus { get; set; }
        public string Type { get; set; }
        public bool IsDeleted { get; set; }
        public IEnumerable<string> MatchingProteinGroups { get; set; }
        public IEnumerable<string> MatchingSerologyNames { get; set; }
    }
}