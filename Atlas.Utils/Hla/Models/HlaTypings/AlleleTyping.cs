using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atlas.Utils.Hla.Models.HlaTypings
{
    internal class AlleleTyping
    {
        private static readonly string ExpressionSuffixPattern = $"[{MolecularTypingNameConstants.ExpressionSuffixes}]$";

        public string AlleleNameWithoutPrefix { get; }
        public string FamilyField { get; }
        public string SubtypeField { get; }
        public string IntronicField { get; }
        public string SilentField { get; }
        public string ExpressionSuffix { get; }

        public AlleleTyping(string alleleName)
        {
            if (string.IsNullOrEmpty(alleleName))
            {
                throw new ArgumentException("Allele name cannot be null or empty.");
            }

            AlleleNameWithoutPrefix = RemoveAllelePrefix(alleleName);

            var fields = GetFields(alleleName).ToList();
            FamilyField = fields[0];
            SubtypeField = fields.Count > 1 ? fields[1] : string.Empty;
            IntronicField = fields.Count > 2 ? fields[2] : string.Empty;
            SilentField = fields.Count > 3 ? fields[3] : string.Empty;

            ExpressionSuffix = GetExpressionSuffix(alleleName);
        }

        public AlleleTyping(string family, string subtype)
            : this(family + MolecularTypingNameConstants.FieldDelimiter + subtype)
        {
        }

        private static string RemoveAllelePrefix(string alleleTyping)
        {
            return alleleTyping.TrimStart(MolecularTypingNameConstants.Prefix);
        }

        private static string GetExpressionSuffix(string alleleName)
        {
            return new Regex(ExpressionSuffixPattern).Match(alleleName).Value;
        }

        private static IEnumerable<string> GetFields(string alleleName)
        {
            var trimmedName = alleleName.TrimEnd(MolecularTypingNameConstants.ExpressionSuffixArray);
            return trimmedName.Split(MolecularTypingNameConstants.FieldDelimiter);
        }
    }
}
