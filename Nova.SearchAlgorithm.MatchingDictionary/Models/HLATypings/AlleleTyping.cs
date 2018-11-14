using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public class AlleleTyping : HlaTyping
    {
        public IEnumerable<string> Fields { get; }
        public string ExpressionSuffix { get; }
        public bool IsNullExpresser { get; }
        public string TwoFieldNameWithExpressionSuffix { get; }
        public string TwoFieldNameWithoutExpressionSuffix { get; }
        public string FirstField { get; }
        public IEnumerable<string> NameVariantsTruncatedByFieldAndOrExpressionSuffix { get; }
        public AlleleTypingStatus Status { get; }

        private const char FieldDelimiter = ':';

        public AlleleTyping(string locus, string name, AlleleTypingStatus status, bool isDeleted = false)
                : base(TypingMethod.Molecular, locus, name, isDeleted)
        {
            Status = status;
            ExpressionSuffix = ExpressionSuffixParser.GetExpressionSuffix(name);
            IsNullExpresser = ExpressionSuffixParser.IsAlleleNull(name);
            Fields = GetFields();
            TwoFieldNameWithExpressionSuffix = BuildAlleleNameAndAddExpressionSuffix(2);
            TwoFieldNameWithoutExpressionSuffix = BuildAlleleNameWithoutExpressionSuffix(2);
            FirstField = Fields.First();
            NameVariantsTruncatedByFieldAndOrExpressionSuffix = GetTruncatedVariantsOfAlleleName();
        }

        public AlleleTyping(MatchLocus matchLocus, string name, bool isDeleted = false)
            : this(matchLocus.ToMolecularLocusIfExists(), name, AlleleTypingStatus.GetDefaultStatus(), isDeleted)
        {
        }

        public bool TryGetThreeFieldName(out string threeFieldName)
        {
            threeFieldName = null;

            if (Fields.Count() < 3)
            {
                return false;
            }

            threeFieldName = string.Join(":", Fields.Take(3));
            return true;
        }

        private IEnumerable<string> GetFields()
        {
            var trimmedName = Name.TrimEnd(ExpressionSuffix.ToCharArray());
            return trimmedName.Split(FieldDelimiter);
        }

        private IEnumerable<string> GetTruncatedVariantsOfAlleleName()
        {
            var alleleNameFieldCounts = new[] { 2, 3, 4 };

            return alleleNameFieldCounts
                .SelectMany(GetAlleleNameVariantsOfSpecifiedFieldCount)
                .Where(variant => !variant.Equals(Name))
                .Distinct();
        }

        private IEnumerable<string> GetAlleleNameVariantsOfSpecifiedFieldCount(int truncatedFieldCount)
        {
            if (Fields.Count() < truncatedFieldCount ||
                (Fields.Count() == truncatedFieldCount && string.IsNullOrEmpty(ExpressionSuffix)))
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
