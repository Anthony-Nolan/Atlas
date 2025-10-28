using System;
using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    public class GradingCalculatorFactoryTests
    {
        [Test]
        public void GetGradingCalculator_WhenBothPatientAndDonorSerology_ReturnsSerologyGradingCalculator()
        {
            var patientScoringInfo = new SerologyScoringInfoBuilder().Build();
            var donorScoringInfo = new SerologyScoringInfoBuilder().Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<SerologyGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void GetGradingCalculator_WhenPatientSerologyAndDonorMolecular_ReturnsSerologyGradingCalculator(
            Type donorScoringInfoType)
        {
            var patientScoringInfo = new SerologyScoringInfoBuilder().Build();
            var donorScoringInfo = BuildScoringInfoOfType(donorScoringInfoType);

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<SerologyGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void GetGradingCalculator_WhenPatientMolecularAndDonorSerology_ReturnsSerologyGradingCalculator(
            Type patientScoringInfoType)
        {
            var patientScoringInfo = BuildScoringInfoOfType(patientScoringInfoType);
            var donorScoringInfo = new SerologyScoringInfoBuilder().Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<SerologyGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void GetGradingCalculator_WhenPatientConsolidatedAndDonorMolecular_ReturnsConsolidatedMolecularGradingCalculator(
            Type donorScoringInfoType)
        {
            var patientScoringInfo = new ConsolidatedMolecularScoringInfoBuilder().Build();
            var donorScoringInfo = BuildScoringInfoOfType(donorScoringInfoType);

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ConsolidatedMolecularGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void GetGradingCalculator_WhenPatientMolecularAndDonorConsolidated_ReturnsConsolidatedMolecularGradingCalculator(
            Type patientScoringInfoType)
        {
            var patientScoringInfo = BuildScoringInfoOfType(patientScoringInfoType);
            var donorScoringInfo = new ConsolidatedMolecularScoringInfoBuilder().Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ConsolidatedMolecularGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        public void GetGradingCalculator_WhenPatientMultipleAlleleAndDonorSingleOrMultipleAllele_ReturnsMultipleAlleleGradingCalculator(
            Type donorScoringInfoType)
        {
            var patientScoringInfo = new MultipleAlleleScoringInfoBuilder().Build();
            var donorScoringInfo = BuildScoringInfoOfType(donorScoringInfoType);

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<MultipleAlleleGradingCalculator>();
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        public void GetGradingCalculator_WhenPatientSingleOrMultipleAlleleAndDonorMultipleAllele_ReturnsMultipleAlleleMolecularGradingCalculator(
            Type patientScoringInfoType)
        {
            var patientScoringInfo = BuildScoringInfoOfType(patientScoringInfoType);
            var donorScoringInfo = new MultipleAlleleScoringInfoBuilder().Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<MultipleAlleleGradingCalculator>();
        }

        [TestCase(typeof(NewAlleleScoringInfo))]
        public void GetGradingCalculator_WhenDonorNewAllele_ReturnsNewAlleleGradingCalculator(
            Type donorScoringInfoType)
        {
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().Build();
            var donorScoringInfo = BuildScoringInfoOfType(donorScoringInfoType);

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<NewAlleleGradingCalculator>();
        }

        [Test]
        public void GetGradingCalculator_WhenBothPatientAndDonorExpressingAllele_ReturnsExpressingAlleleGradingCalculator()
        {
            const string expressingAlleleName = "01:01";
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(expressingAlleleName).Build();
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(expressingAlleleName).Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ExpressingAlleleGradingCalculator>();
        }

        [Test]
        public void GetGradingCalculator_WhenBothPatientAndDonorNullAllele_ReturnsNullAlleleGradingCalculator()
        {
            const string nullAlleleName = "01:01N";
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(nullAlleleName).Build();
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(nullAlleleName).Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<NullAlleleGradingCalculator>();
        }

        [TestCase("01:01", "01:01N")]
        [TestCase("01:01N", "01:01")]
        public void GetGradingCalculator_WhenOneAlleleExpressingAndOtherNull_ReturnsExpressingVsNullAlleleGradingCalculator(
            string patientAlleleName,
            string donorAlleleName)
        {
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(patientAlleleName).Build();
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(donorAlleleName).Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ExpressingVsNullAlleleGradingCalculator>();
        }

        [Test]
        public void GetGradingCalculator_WhenOneAlleleConsolidatedAndOtherNull_ReturnsExpressingVsNullAlleleGradingCalculator()
        {
            const string nullAlleleName = "01:01N";
            var patientScoringInfo = new ConsolidatedMolecularScoringInfoBuilder().Build();
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(nullAlleleName).Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ExpressingVsNullAlleleGradingCalculator>();
        }

        [Test]
        public void GetGradingCalculator_WhenOneHlaMultipleAllelesAndOtherNull_ReturnsExpressingVsNullAlleleGradingCalculator()
        {
            const string nullAlleleName = "01:01N";
            var patientScoringInfo = new MultipleAlleleScoringInfoBuilder().Build();
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(nullAlleleName).Build();

            var calculator = GradingCalculatorFactory.GetGradingCalculator(patientScoringInfo, donorScoringInfo);

            calculator.Should().BeOfType<ExpressingVsNullAlleleGradingCalculator>();
        }

        private static IHlaScoringInfo BuildScoringInfoOfType(Type scoringInfoType)
        {
            if (scoringInfoType == typeof(SingleAlleleScoringInfo))
            {
                return new SingleAlleleScoringInfoBuilder().Build();
            }

            if (scoringInfoType == typeof(ConsolidatedMolecularScoringInfo))
            {
                return new ConsolidatedMolecularScoringInfoBuilder().Build();
            }

            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new List<SingleAlleleScoringInfo>
                    {
                        new SingleAlleleScoringInfoBuilder().Build()
                    })
                    .Build();
            }

            if (scoringInfoType == typeof(SerologyScoringInfo))
            {
                return new SerologyScoringInfoBuilder().Build();
            }

            if (scoringInfoType == typeof(NewAlleleScoringInfo))
            {
                return new NewAlleleScoringInfoBuilder().Build();
            }

            throw new Exception($"Unsupported type: {scoringInfoType}");
        }
    }
}