using System;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters
{
    public class MatchLociFilter
    {
        public Func<IWmdaHlaType, bool> Filter { get; set; }
        protected List<string> MatchLoci { get; set; }
    }
}
