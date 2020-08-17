using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;

namespace Atlas.Common.GeneticData.Hla.Services
{
    /// <summary>
    /// Note: no checks are made on the HLA categories of the submitted allele strings.
    /// It is up to the consumer to validate the HLA before calling any of these methods.
    /// </summary>
    public static class AlleleStringSplitter
    {
        /// <param name="alleleString">Should be allele string of style: "01:01/02:02:02/03:03:03:03",
        /// i.e., Every allele in string is fully named. Each allele can have different first fields, as well as field counts.</param>
        public static IEnumerable<string> SplitAlleleStringOfNamesToAlleleNames(string alleleString)
        {
            return SplitAlleleString(alleleString)
                .Select(str => new MolecularAlleleDetails(str))
                .Select(allele => allele.AlleleNameWithoutPrefix);
        }

        /// <param name="alleleString">Should be allele string of format: "01:01/02/03".
        /// I.e., All alleles share the same first field, and only second fields (subtypes) are listed in the delimited string.</param>
        public static IEnumerable<string> SplitAlleleStringOfSubtypesToAlleleNames(string alleleString)
        {
            var split = SplitAlleleString(alleleString).ToList();

            if (split.Count < 2)
            {
                throw new ArgumentException($"Submitted value, {alleleString}, is not a valid allele string.");
            }

            var firstAllele = new MolecularAlleleDetails(split[0]);

            var alleles = new List<MolecularAlleleDetails> { firstAllele };

            alleles.AddRange(split
                    .Skip(1)
                    .Select(subtype => new MolecularAlleleDetails(firstAllele.FamilyField, subtype)));

            return alleles.Select(allele => allele.AlleleNameWithoutPrefix);
        }

        /// <summary>
        /// Splits <paramref name="alleleString"/> by the standard allele delimiter ("/") without any further modifications.
        /// </summary>
        public static IEnumerable<string> SplitAlleleString(string alleleString)
        {
            const char alleleStringDelimiter = '/';
            return alleleString.Split(alleleStringDelimiter);
        }
    }
}