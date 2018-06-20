using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public class AlleleTyping : HlaTyping
    {
        public IEnumerable<string> Fields { get; }
        public string ExpressionSuffix { get; }
        public bool IsNullExpresser { get; }
        public string TwoFieldName { get; }
        public AlleleTypingStatus Status { get; }

        private static readonly string[] NullExpressionSuffixes = { "N" };

        public AlleleTyping(string locus, string name, AlleleTypingStatus status, bool isDeleted = false)
                : base(TypingMethod.Molecular, locus, name, isDeleted)
        {
            Status = status;
            ExpressionSuffix = GetExpressionSuffix(name);
            IsNullExpresser = NullExpressionSuffixes.Contains(ExpressionSuffix);
            Fields = GetFields(name, ExpressionSuffix);
            TwoFieldName = GetTwoFieldName(Fields, ExpressionSuffix, name);
        }

        public AlleleTyping(string locus, string name, bool isDeleted = false)
            :this(locus, name, AlleleTypingStatus.GetDefaultStatus(), isDeleted)
        {            
        }

        private static string GetExpressionSuffix(string name)
        {
            return new Regex(@"[A-Z]$").Match(name).Value;
        }

        private static IEnumerable<string> GetFields(string name, string expressionSuffix)
        {
            var trimmedName = name.TrimEnd(expressionSuffix.ToCharArray());
            return trimmedName.Split(':');
        }

        private static string GetTwoFieldName(IEnumerable<string> fields, string expressionSuffix, string name)
        {
            var fieldsList = fields.ToList();
            return fieldsList.Count >= 2 ? $"{fieldsList[0]}:{fieldsList[1]}{expressionSuffix}" : name;
        }
    }
}
