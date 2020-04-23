using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Hla.Models.HlaTypings;

namespace Atlas.Utils.Hla.Services.AlleleStringSplitters
{
    internal class AlleleStringOfNamesSplitter : AlleleStringSplitterBase
    {
        protected override IEnumerable<AlleleTyping> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString)
        {
            return splitAlleleString.Select(str => new AlleleTyping(str));
        }
    }
}
