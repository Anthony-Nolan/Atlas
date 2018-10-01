using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    [TestFixture]
    public class AlleleTypingTest
    {
        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenNew_TypingMethodIsMolecular(object[] alleleToTest)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.TypingMethod.Should().Be(TypingMethod.Molecular);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenIsDeletedValueNotSupplied_IsDeletedSetToFalse(object[] alleleToTest)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.IsDeleted.Should().Be(false);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenAlleleStatusNotSupplied_AlleleStatusSetToUnknown(object[] alleleToTest)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.Status.SequenceStatus.Should().Be(SequenceStatus.Unknown);
            actualAlleleTyping.Status.DnaCategory.Should().Be(DnaCategory.Unknown);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedMolecularLocus))]
        public void AlleleTyping_WhenNew_MatchLocusConvertedToMolecularLocusName(object[] alleleToTest, string expectedMolecularLocus)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.Locus.Should().Be(expectedMolecularLocus);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedExpressionSuffixes))]
        public void AlleleTyping_WhenNew_ExpressionSuffixSetCorrectly(object[] alleleToTest, string expectedExpressionSuffix)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.ExpressionSuffix.Should().Be(expectedExpressionSuffix);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedIsNullExpresser))]
        public void AlleleTyping_WhenNew_IsNullExpresserSetCorrectly(object[] alleleToTest, bool expectedIsNullExpresser)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.IsNullExpresser.Should().Be(expectedIsNullExpresser);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFields))]
        public void AlleleTyping_WhenNew_FieldsSetCorrectly(object[] alleleToTest, IEnumerable<string> expectedFields)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.Fields.ShouldBeEquivalentTo(expectedFields);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithExpressionSuffixSetCorrectly(object[] alleleToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.TwoFieldNameWithExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithoutExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithoutExpressionSuffixSetCorrectly(object[] alleleToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.TwoFieldNameWithoutExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFirstField))]
        public void AlleleTyping_WhenNew_FirstFieldSetCorrectly(object[] alleleToTest, string expectedFirstField)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.FirstField.Should().Be(expectedFirstField);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedAlleleNameVariants))]
        public void AlleleTyping_WhenNew_AlleleNameVariantsSetCorrectly(object[] alleleToTest, IEnumerable<string> expectedAlleleNameVariants)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(expectedAlleleNameVariants);
        }

        private static AlleleTyping GetActualAlleleTyping(IReadOnlyList<object> alleleToTest)
        {
            return new AlleleTyping((MatchLocus)alleleToTest[0], GetAlleleName(alleleToTest));
        }

        private static string GetAlleleName(IReadOnlyList<object> alleleToTest)
        {
            return alleleToTest[1].ToString();
        }
    }
}