using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;

namespace Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters
{
    public abstract class AlleleStringSplitterBase
    {
        private const char AlleleStringDelimiter = '/';

        public IEnumerable<string> GetAlleleNamesFromAlleleString(string alleleString)
        {
            var splitAlleleString = alleleString.Split(AlleleStringDelimiter);

            var alleles = GetAlleleTypingsFromSplitAlleleString(splitAlleleString);

            return alleles.Select(allele => allele.AlleleNameWithoutPrefix);
        }

        protected abstract IEnumerable<MolecularAlleleDetails> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString);
    }
}
