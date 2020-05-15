using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;

namespace Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters
{
    internal class AlleleStringOfNamesSplitter : AlleleStringSplitterBase
    {
        protected override IEnumerable<MolecularAlleleDetails> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString)
        {
            return splitAlleleString.Select(str => new MolecularAlleleDetails(str));
        }
    }
}
