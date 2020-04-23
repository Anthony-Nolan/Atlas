using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Common.Models.HlaTypings;

namespace Atlas.MatchingAlgorithm.Common.Services.AlleleStringSplitters
{
    internal class AlleleStringOfNamesSplitter : AlleleStringSplitterBase
    {
        protected override IEnumerable<AlleleTyping> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString)
        {
            return splitAlleleString.Select(str => new AlleleTyping(str));
        }
    }
}
