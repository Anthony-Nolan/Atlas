using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils
{
    public static class AlleleSplitter
    {
        private static readonly Regex ExpressionSuffixRegex =
            new Regex(MolecularTypingNameConstants.ExpressionSuffixesRegexCharacterGroup, RegexOptions.Compiled);

        public static int NumberOfFields(string allele) => SplitToFields(allele).Count();

        public static IEnumerable<string> FirstThreeFields(string allele) => SplitToFields(allele).Take(3);

        public static string FirstThreeFieldsAsString(string allele) =>
            FirstThreeFields(allele).StringJoin(MolecularTypingNameConstants.FieldDelimiter);

        public static IEnumerable<string> FirstTwoFields(string allele) => SplitToFields(allele).Take(2);

        public static string FirstTwoFieldsAsString(string allele) => FirstTwoFields(allele).StringJoin(MolecularTypingNameConstants.FieldDelimiter);

        public static string RemoveLastField(string allele)
        {
            var splitAllele = SplitToFields(allele).ToList();
            return splitAllele.Take(splitAllele.Count - 1).StringJoin(MolecularTypingNameConstants.FieldDelimiter);
        }

        public static string FirstField(string allele) => SplitToFields(allele).First();

        public static string SecondField(string allele) => SplitToFields(allele).ToList()[1];

        public static string RemovePrefix(string allele) => allele.TrimStart(MolecularTypingNameConstants.Prefix);

        public static string GetExpressionSuffix(string allele) => ExpressionSuffixRegex.Match(allele).Value;

        internal static IEnumerable<string> SplitToFields(string alleleName)
        {
            // TODO: NOVA-1571: Handle alleles with an expression suffix. This truncation will remove expression suffix.
            var trimmedName = alleleName.TrimEnd(MolecularTypingNameConstants.ExpressionSuffixArray);
            return trimmedName.Split(MolecularTypingNameConstants.FieldDelimiter);
        }
    }
}