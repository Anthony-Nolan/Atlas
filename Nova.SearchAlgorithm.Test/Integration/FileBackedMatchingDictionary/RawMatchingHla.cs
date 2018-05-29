using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Test.FileBackedMatchingDictionary
{
    public class RawMatchingHla
    {
        public string Locus { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> MatchingPGroups { get; set; }
        public List<RawMatchingSerology> MatchingSerology { get; set; }
    }

    public class RawMatchingSerology
    {
        public string Name { get; set; }
        public int Subtype{ get; set; }
    }
}