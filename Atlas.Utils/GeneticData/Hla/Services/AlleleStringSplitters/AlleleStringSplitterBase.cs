using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Hla.Models.HlaTypings;

namespace Atlas.Utils.Hla.Services.AlleleStringSplitters
{
    internal abstract class AlleleStringSplitterBase
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
