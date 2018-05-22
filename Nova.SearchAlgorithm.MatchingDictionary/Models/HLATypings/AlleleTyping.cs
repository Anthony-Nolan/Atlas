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

        public AlleleTyping(string wmdaLocus, string name, bool isDeleted = false) : base(wmdaLocus, name, isDeleted)
        {
            ExpressionSuffix = GetExpressionSuffix(name);
            IsNullExpresser = AlleleExpression.NullExpressionSuffixes.Contains(ExpressionSuffix);

            var fields = GetFields(name, ExpressionSuffix).ToList();
            Fields = fields;
            TwoFieldName = fields.Count >= 2 ? $"{fields.ElementAt(0)}:{fields.ElementAt(1)}{ExpressionSuffix}" : name;
        }

        public AlleleTyping(AlleleTyping alleleTyping) : this(alleleTyping.WmdaLocus, alleleTyping.Name, alleleTyping.IsDeleted)
        {
        }

        private static string GetExpressionSuffix(string hlaName)
        {
            return new Regex(@"[A-Z]$").Match(hlaName).Value;
        }

        private static IEnumerable<string> GetFields(string hlaName, string expressionSuffix)
        {
            var name = expressionSuffix.Equals("") ? hlaName : hlaName.Split(expressionSuffix[0])[0];
            return name.Split(':');
        }
    }
}
