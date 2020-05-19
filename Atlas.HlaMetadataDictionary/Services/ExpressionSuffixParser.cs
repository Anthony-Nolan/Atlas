using System.Linq;
using System.Text.RegularExpressions;

namespace Atlas.HlaMetadataDictionary.Services
{
    internal static class ExpressionSuffixParser
    {
        // This is done instead of Regex matching for performance reasons
        private static readonly char[] AllSuffixes =
        {
            'A',
            'B',
            'C',
            'D',
            'E',
            'F',
            'G',
            'H',
            'I',
            'J',
            'K',
            'L',
            'M',
            'N',
            'O',
            'P',
            'Q',
            'R',
            'S',
            'T',
            'U',
            'V',
            'W',
            'X',
            'Y',
            'Z',
        };

        private const char NullExpressionSuffix = 'N';

        public static string GetExpressionSuffix(string name)
        {
            var finalCharacter = name[name.Length - 1];
            return AllSuffixes.Contains(finalCharacter) ? finalCharacter.ToString() : "";
        }
        
        public static bool IsAlleleNull(string name)
        {
            var finalCharacter = name[name.Length - 1];
            return NullExpressionSuffix == finalCharacter;
        }
    }
}