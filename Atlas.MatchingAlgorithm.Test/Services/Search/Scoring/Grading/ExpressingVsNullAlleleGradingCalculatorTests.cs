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
    public class ExpressingVsNullAlleleGradingCalculatorTests : GradingCalculatorTestsBase
    {
        [SetUp]
        public override void SetUpGradingCalculator()
        {
            GradingCalculator = new ExpressingVsNullAlleleGradingCalculator();
        }

        #region Tests: Exception Cases

        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SerologyScoringInfo))]
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

        #region Tests: One Allele Expressing & Other Null

        [TestCase("999:999", "999:999N")]
        [TestCase("999:999N", "999:999")]
        public void CalculateGrade_OneAlleleIsExpressingAndOtherIsNullExpresser_ReturnsMismatch(
            string patientAlleleName,
            string donorAlleleName)
        {
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.ExpressingVsNull);
        }

        #endregion
    }
}