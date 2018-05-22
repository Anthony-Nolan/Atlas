using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class ExpandedHla
    {
        public string Name { get; set; }
        public string Locus { get; set; }
        public IEnumerable<string> PGroups { get; set; }
    }
}