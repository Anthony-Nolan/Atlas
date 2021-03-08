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

        /// <returns>
        /// 1) When 2f allele, returns {first field, second field},
        ///     e.g., 01:01 => {"01","01"}
        /// 2) When >2f typing, returns {first field, second field},
        ///     e.g., 01:01:01:01 => {"01","01"}; 01:01:01G => {"01","01"}
        /// 3) When HLA typing with 2f+suffix, returns {first field, second field+suffix},
        ///     e.g., 01:01N => {"01","01N"}; 01:01g => {"01","01g"}; 01:01P => {"01","01P"}
        /// </returns>
        public static IEnumerable<string> FirstTwoFields(string allele) => SplitToFields(allele).Take(2);

        /// <returns>Simple concatenation of <see cref="FirstTwoFields"/> separated by a field delimiter.</returns>
        public static string FirstTwoFieldsAsString(string allele) => FirstTwoFields(allele).StringJoin(MolecularTypingNameConstants.FieldDelimiter);

        public static string FirstTwoFieldsWithExpressionSuffixAsString(string allele) =>
            NumberOfFields(allele) > 2
                ? FirstTwoFields(allele).StringJoin(MolecularTypingNameConstants.FieldDelimiter) + GetExpressionSuffix(allele)
                : allele;

        /// <returns>
        /// <see cref="FirstTwoFieldsAsString"/> with the molecular typing suffix stripped out.
        /// E.g., 01:01N => 01:01; 01:01g => 01:01; 01:01P => 01:01
        /// </returns>
        public static string FirstTwoFieldsAsStringWithSuffixRemoved(string allele) =>
            RemoveMolecularSuffix(FirstTwoFieldsAsString(allele));

        public static string RemoveLastField(string allele)
        {
            var splitAllele = SplitToFields(allele).ToList();
            return splitAllele.Take(splitAllele.Count - 1).StringJoin(MolecularTypingNameConstants.FieldDelimiter);
        }

        public static string FirstField(string allele) => SplitToFields(allele).First();

        public static string SecondField(string allele) => SplitToFields(allele).ToList()[1];

        public static string SecondFieldWithSuffixRemoved(string allele) => RemoveMolecularSuffix(SecondField(allele));

        public static string RemovePrefix(string allele) => allele.TrimStart(MolecularTypingNameConstants.Prefix);

        public static string GetExpressionSuffix(string allele) => ExpressionSuffixRegex.Match(allele).Value;

        internal static IEnumerable<string> SplitToFields(string alleleName)
        {
            return alleleName.Split(MolecularTypingNameConstants.FieldDelimiter);
        }

        private static string RemoveMolecularSuffix(string hla)
        {
            return hla.TrimEnd(MolecularTypingNameConstants.AllPossibleSuffixes);
        }
    }
}