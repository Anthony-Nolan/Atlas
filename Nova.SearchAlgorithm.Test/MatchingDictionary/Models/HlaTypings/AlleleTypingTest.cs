using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;
using System.Collections.Generic;

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

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameSetCorrectly(object[] alleleToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.TwoFieldName.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedAlleleNameVariants))]
        public void AlleleTyping_WhenNew_AlleleNameVariantsSetCorrectly(object[] alleleToTest, IEnumerable<string> expectedAlleleNameVariants)
        {
            var actualAlleleTyping = GetActualAlleleTyping(alleleToTest);

            actualAlleleTyping.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(expectedAlleleNameVariants);
        }

        private static AlleleTyping GetActualAlleleTyping(IReadOnlyList<object> alleleToTest)
        {
            return new AlleleTyping((MatchLocus)alleleToTest[0], alleleToTest[1].ToString());
        }
    }
}