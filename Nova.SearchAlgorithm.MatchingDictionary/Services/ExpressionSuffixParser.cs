using System.Linq;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public static class ExpressionSuffixParser
    {
        private static readonly string[] NullExpressionSuffixes = { "N" };
        private static readonly Regex SuffixRegex = new Regex(@"[A-Z]$");

        public static string GetExpressionSuffix(string name)
        {
            return SuffixRegex.Match(name).Value;
        }
        
        public static bool IsAlleleNull(string name)
        {
            var expressionSuffix = GetExpressionSuffix(name);
            return NullExpressionSuffixes.Contains(expressionSuffix);
        }
    }
}