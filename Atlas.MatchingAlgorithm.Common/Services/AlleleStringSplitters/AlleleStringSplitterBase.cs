using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Common.Models.HlaTypings;

namespace Atlas.MatchingAlgorithm.Common.Services.AlleleStringSplitters
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

        protected abstract IEnumerable<AlleleTyping> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString);
    }
}
