using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Hla.Models.HlaTypings;

namespace Atlas.Utils.Hla.Services.AlleleStringSplitters
{
    internal class AlleleStringOfSubtypesSplitter : AlleleStringSplitterBase
    {
        protected override IEnumerable<AlleleTyping> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString)
        {
            var splitAlleleStringList = splitAlleleString.ToList();

            if (splitAlleleStringList.Count < 2)
            {
                throw new ArgumentException($"Submitted value, {splitAlleleString}, is not a valid allele string.");
            }

            var firstAllele = new AlleleTyping(splitAlleleStringList[0]);

            var alleles = new List<AlleleTyping> { firstAllele };
            alleles.AddRange(
                splitAlleleStringList
                    .Skip(1)
                    .Select(subtype => new AlleleTyping(firstAllele.FamilyField, subtype)));

            return alleles;
        }
    }
}
