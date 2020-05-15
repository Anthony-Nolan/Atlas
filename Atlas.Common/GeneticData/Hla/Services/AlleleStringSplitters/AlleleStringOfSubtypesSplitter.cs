using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;

namespace Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters
{
    internal class AlleleStringOfSubtypesSplitter : AlleleStringSplitterBase
    {
        protected override IEnumerable<MolecularAlleleDetails> GetAlleleTypingsFromSplitAlleleString(IEnumerable<string> splitAlleleString)
        {
            var splitAlleleStringList = splitAlleleString.ToList();

            if (splitAlleleStringList.Count < 2)
            {
                throw new ArgumentException($"Submitted value, {splitAlleleString}, is not a valid allele string.");
            }

            var firstAllele = new MolecularAlleleDetails(splitAlleleStringList[0]);

            var alleles = new List<MolecularAlleleDetails> { firstAllele };
            alleles.AddRange(
                splitAlleleStringList
                    .Skip(1)
                    .Select(subtype => new MolecularAlleleDetails(firstAllele.FamilyField, subtype)));

            return alleles;
        }
    }
}
