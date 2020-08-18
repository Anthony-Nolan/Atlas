using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using FluentAssertions;
using NUnit.Framework;
using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
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
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(patientScoringInfoType))
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
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
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var donorLookupResult = new HlaScoringMetadataBuilder()
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
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);
            var donorLookupResult = new HlaScoringMetadataBuilder()
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
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .WithMatchingPGroup(null).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Partial, dnaCategory);
            var donorLookupResult = new HlaScoringMetadataBuilder()
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
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingPGroup(null)
                    .Build())
                .Build();

            const string donorAlleleName = "999:999N";
            var donorLookupResult = new HlaScoringMetadataBuilder()
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