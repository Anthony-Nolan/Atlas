using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping
{
    public static class MolecularTypingNameConstants
    {
        public const char Prefix = '*';
        public const char FieldDelimiter = ':';
        public static readonly char[] ExpressionSuffixArray = { 'N', 'C', 'S', 'L', 'Q', 'A' };
        public static readonly string ExpressionSuffixesRegexCharacterGroup = $"[{new string(ExpressionSuffixArray)}]"; //i.e. [NCSLQA]
        public static readonly char[] AlleleGroupSuffixArray = {'G', 'P', 'g'};
        public static readonly char[] AllPossibleSuffixes = new List<char>(ExpressionSuffixArray).Concat(AlleleGroupSuffixArray).ToArray();
    }
}
