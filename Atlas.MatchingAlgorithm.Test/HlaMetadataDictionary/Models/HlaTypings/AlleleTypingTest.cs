using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaMatchPreCalculation;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Models.HlaTypings
{
    [TestFixture]
    public class AlleleTypingTest
    {
        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenNew_TypingMethodIsMolecular(AlleleTestCase alleleTestCaseToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.TypingMethod.Should().Be(TypingMethod.Molecular);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenIsDeletedValueNotSupplied_IsDeletedSetToFalse(AlleleTestCase alleleTestCaseToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.IsDeleted.Should().Be(false);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenAlleleStatusNotSupplied_AlleleStatusSetToUnknown(AlleleTestCase alleleTestCaseToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);
            
            actualAlleleTyping.Status.SequenceStatus.Should().Be(SequenceStatus.Unknown);
            actualAlleleTyping.Status.DnaCategory.Should().Be(DnaCategory.Unknown);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedMolecularLocus))]
        public void AlleleTyping_WhenNew_LocusConvertedToTypingLocusName(AlleleTestCase alleleTestCaseToTest, string expectedTypingLocus)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.TypingLocus.Should().Be(expectedTypingLocus);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedExpressionSuffixes))]
        public void AlleleTyping_WhenNew_ExpressionSuffixSetCorrectly(AlleleTestCase alleleTestCaseToTest, string expectedExpressionSuffix)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.ExpressionSuffix.Should().Be(expectedExpressionSuffix);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedIsNullExpresser))]
        public void AlleleTyping_WhenNew_IsNullExpresserSetCorrectly(AlleleTestCase alleleTestCaseToTest, bool expectedIsNullExpresser)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.IsNullExpresser.Should().Be(expectedIsNullExpresser);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFields))]
        public void AlleleTyping_WhenNew_FieldsSetCorrectly(AlleleTestCase alleleTestCaseToTest, IEnumerable<string> expectedFields)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.Fields.ShouldBeEquivalentTo(expectedFields);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithExpressionSuffixSetCorrectly(AlleleTestCase alleleTestCaseToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.TwoFieldNameWithExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithoutExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithoutExpressionSuffixSetCorrectly(AlleleTestCase alleleTestCaseToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.TwoFieldNameWithoutExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFirstField))]
        public void AlleleTyping_WhenNew_FirstFieldSetCorrectly(AlleleTestCase alleleTestCaseToTest, string expectedFirstField)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.FirstField.Should().Be(expectedFirstField);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedAlleleNameVariants))]
        public void AlleleTyping_WhenNew_AlleleNameVariantsSetCorrectly(AlleleTestCase alleleTestCaseToTest, IEnumerable<string> expectedAlleleNameVariants)
        {
            var actualAlleleTyping = new AlleleTyping(alleleTestCaseToTest.Locus, alleleTestCaseToTest.Name);

            actualAlleleTyping.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(expectedAlleleNameVariants);
        }
    }
}