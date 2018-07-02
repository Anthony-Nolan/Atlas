using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public class AlleleTyping : HlaTyping
    {
        public IEnumerable<string> Fields { get; }
        public string ExpressionSuffix { get; }
        public bool IsNullExpresser { get; }
        public string TwoFieldName { get; }
        public IEnumerable<string> NameVariantsTruncatedByFieldAndOrExpressionSuffix { get; }
        public AlleleTypingStatus Status { get; }

        private const char FieldDelimiter = ':';
        private static readonly string[] NullExpressionSuffixes = { "N" };

        public AlleleTyping(string locus, string name, AlleleTypingStatus status, bool isDeleted = false)
                : base(TypingMethod.Molecular, locus, name, isDeleted)
        {
            Status = status;
            ExpressionSuffix = GetExpressionSuffix();
            IsNullExpresser = NullExpressionSuffixes.Contains(ExpressionSuffix);
            Fields = GetFields();
            TwoFieldName = BuildAlleleNameAndAddExpressionSuffix(2);
            NameVariantsTruncatedByFieldAndOrExpressionSuffix = GetTruncatedVariantsOfAlleleName();
        }

        public AlleleTyping(MatchLocus matchLocus, string name, bool isDeleted = false)
            : this(matchLocus.ToMolecularLocusNameIfExists(), name, AlleleTypingStatus.GetDefaultStatus(), isDeleted)
        {
        }

        private string GetExpressionSuffix()
        {
            return new Regex(@"[A-Z]$").Match(Name).Value;
        }

        private IEnumerable<string> GetFields()
        {
            var trimmedName = Name.TrimEnd(ExpressionSuffix.ToCharArray());
            return trimmedName.Split(FieldDelimiter);
        }

        private IEnumerable<string> GetTruncatedVariantsOfAlleleName()
        {
            var threeFieldVariants = GetAlleleNameVariantsOfSpecifiedFieldCount(3);
            var twoFieldVariants = GetAlleleNameVariantsOfSpecifiedFieldCount(2);

            return twoFieldVariants.Union(threeFieldVariants);
        }

        private IEnumerable<string> GetAlleleNameVariantsOfSpecifiedFieldCount(int truncatedFieldCount)
        {
            if (Fields.Count() < truncatedFieldCount ||
                Fields.Count() == truncatedFieldCount && string.IsNullOrEmpty(ExpressionSuffix))
            {
                return new List<string>();
            }

            var variants = new List<string>
            {
                BuildAlleleNameWithoutExpressionSuffix(truncatedFieldCount),
                BuildAlleleNameAndAddExpressionSuffix(truncatedFieldCount)
            };

            return variants.Distinct();
        }

        private string BuildAlleleNameAndAddExpressionSuffix(int fieldCount)
        {
            return BuildAlleleNameWithoutExpressionSuffix(fieldCount) + ExpressionSuffix;
        }

        private string BuildAlleleNameWithoutExpressionSuffix(int fieldCount)
        {
            return string.Join(FieldDelimiter.ToString(), Fields.Take(fieldCount));
        }
    }
}
