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

        private static readonly string[] NullExpressionSuffixes = { "N" };

    public AlleleTyping(string wmdaLocus, string name, bool isDeleted = false) 
            : base(TypingMethod.Molecular, wmdaLocus, name, isDeleted)
        {
            ExpressionSuffix = GetExpressionSuffix(name);
            IsNullExpresser = NullExpressionSuffixes.Contains(ExpressionSuffix);
            Fields = GetFields(name, ExpressionSuffix);
            TwoFieldName = GetTwoFieldName(Fields, ExpressionSuffix, name);
        }

        public AlleleTyping(AlleleTyping alleleTyping) : this(alleleTyping.Locus, alleleTyping.Name, alleleTyping.IsDeleted)
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
            var fieldsArr = fields.ToArray();
            return fieldsArr.Length >= 2 ? $"{fieldsArr[0]}:{fieldsArr[1]}{expressionSuffix}" : name;
        }
    }
}
