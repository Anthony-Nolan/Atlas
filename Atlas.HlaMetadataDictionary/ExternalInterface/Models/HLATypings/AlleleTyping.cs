using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.Services;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings
{
    public class AlleleTyping : HlaTyping
    {
        public IEnumerable<string> Fields { get; }
        public string ExpressionSuffix { get; }
        public bool IsNullExpresser { get; }
        public string TwoFieldNameIncludingExpressionSuffix { get; }
        public string TwoFieldNameExcludingExpressionSuffix { get; }
        public string FirstField { get; }
        public IEnumerable<string> NameVariantsTruncatedByFieldAndOrExpressionSuffix { get; }
        public AlleleTypingStatus Status { get; }

        private const char FieldDelimiter = ':';

        internal AlleleTyping(string typingLocus, string name, AlleleTypingStatus status, bool isDeleted = false)
                : base(TypingMethod.Molecular, typingLocus, name, isDeleted)
        {
            Status = status;
            ExpressionSuffix = ExpressionSuffixParser.GetExpressionSuffix(name);
            IsNullExpresser = ExpressionSuffixParser.IsAlleleNull(name);
            Fields = GetFields();
            TwoFieldNameIncludingExpressionSuffix = BuildAlleleNameAndAddExpressionSuffix(2);
            TwoFieldNameExcludingExpressionSuffix = BuildAlleleNameWithoutExpressionSuffix(2);
            FirstField = Fields.First();
            NameVariantsTruncatedByFieldAndOrExpressionSuffix = GetTruncatedVariantsOfAlleleName();
        }

        internal AlleleTyping(Locus locus, string name, AlleleTypingStatus status = null)
            : this(locus.ToMolecularLocusIfExists(), name, status ?? AlleleTypingStatus.GetDefaultStatus())
        {
        }

        public bool TryGetThreeFieldName(out string threeFieldName)
        {
            threeFieldName = null;

            if (Fields.Count() < 3)
            {
                return false;
            }

            threeFieldName = BuildAlleleNameWithoutExpressionSuffix(3);
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
            return Fields.Take(fieldCount).StringJoin(FieldDelimiter);
        }
    }
}
