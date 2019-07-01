using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;
using System;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    [TestFixture]
    public class NullAlleleGradingCalculatorTests : GradingCalculatorTestsBase
    {
        [SetUp]
        public override void SetUpGradingCalculator()
        {
            GradingCalculator = new NullAlleleGradingCalculator();
        }

        #region Tests: Exception Cases

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        public override void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType
            )
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(patientScoringInfoType))
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(donorScoringInfoType))
                .Build();

            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult));
        }

        #endregion

        #region Tests: Both Alleles Are Null Expressing

        [Test]
        public void CalculateGrade_BothTypingsAreNullAlleles_WithSameName_AndFullGDnaSequences_ReturnsNullGDna()
        {
            const string sharedAlleleName = "999:999N";

            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.NullGDna);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreNullAlleles_WithSameName_AndFullCDnaSequences_ReturnsNullCDna()
        {
            const string sharedAlleleName = "999:999N";

            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.NullCDna);
        }

        [TestCase(DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna)]
        public void CalculateGrade_BothTypingsAreNullAlleles_WithSameName_AndPartialSequences_ReturnsNullPartial(
            DnaCategory dnaCategory)
        {
            const string sharedAlleleName = "999:999N";

            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Partial, dnaCategory);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Partial, dnaCategory);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.NullPartial);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreNullAlleles_WithDifferentNames_ReturnsNullMismatch()
        {
            const string patientAlleleName = "111:111N";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingPGroup(null)
                    .Build())
                .Build();

            const string donorAlleleName = "999:999N";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingPGroup(null)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.NullMismatch);
        }

        #endregion
    }
}