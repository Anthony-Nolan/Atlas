using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    [TestFixture]
    public class AlleleTypingTest
    {
        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenNew_TypingMethodIsMolecular(Allele alleleToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.TypingMethod.Should().Be(TypingMethod.Molecular);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenIsDeletedValueNotSupplied_IsDeletedSetToFalse(Allele alleleToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.IsDeleted.Should().Be(false);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.AlleleTypingsToTest))]
        public void AlleleTyping_WhenAlleleStatusNotSupplied_AlleleStatusSetToUnknown(Allele alleleToTest)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);
            
            actualAlleleTyping.Status.SequenceStatus.Should().Be(SequenceStatus.Unknown);
            actualAlleleTyping.Status.DnaCategory.Should().Be(DnaCategory.Unknown);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedMolecularLocus))]
        public void AlleleTyping_WhenNew_LocusConvertedToTypingLocusName(Allele alleleToTest, string expectedTypingLocus)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.TypingLocus.Should().Be(expectedTypingLocus);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedExpressionSuffixes))]
        public void AlleleTyping_WhenNew_ExpressionSuffixSetCorrectly(Allele alleleToTest, string expectedExpressionSuffix)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.ExpressionSuffix.Should().Be(expectedExpressionSuffix);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedIsNullExpresser))]
        public void AlleleTyping_WhenNew_IsNullExpresserSetCorrectly(Allele alleleToTest, bool expectedIsNullExpresser)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.IsNullExpresser.Should().Be(expectedIsNullExpresser);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFields))]
        public void AlleleTyping_WhenNew_FieldsSetCorrectly(Allele alleleToTest, IEnumerable<string> expectedFields)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.Fields.ShouldBeEquivalentTo(expectedFields);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithExpressionSuffixSetCorrectly(Allele alleleToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.TwoFieldNameWithExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedTwoFieldWithoutExpressionSuffixNames))]
        public void AlleleTyping_WhenNew_TwoFieldNameWithoutExpressionSuffixSetCorrectly(Allele alleleToTest, string expectedTwoFieldName)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.TwoFieldNameWithoutExpressionSuffix.Should().Be(expectedTwoFieldName);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedFirstField))]
        public void AlleleTyping_WhenNew_FirstFieldSetCorrectly(Allele alleleToTest, string expectedFirstField)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.FirstField.Should().Be(expectedFirstField);
        }

        [TestCaseSource(typeof(AlleleTypingTestCaseSources), nameof(AlleleTypingTestCaseSources.ExpectedAlleleNameVariants))]
        public void AlleleTyping_WhenNew_AlleleNameVariantsSetCorrectly(Allele alleleToTest, IEnumerable<string> expectedAlleleNameVariants)
        {
            var actualAlleleTyping = new AlleleTyping(alleleToTest.Locus, alleleToTest.Name);

            actualAlleleTyping.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(expectedAlleleNameVariants);
        }
    }
}