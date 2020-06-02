using System;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    public abstract class GradingCalculatorTestsBase
    {
        protected IGradingCalculator GradingCalculator;

        [SetUp]
        public abstract void SetUpGradingCalculator();

        [Test]
        public void CalculateGrade_MatchLociAreNotEqual_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(
                    new HlaScoringMetadataBuilder()
                        .AtLocus(Locus.A)
                        .Build(),
                    new HlaScoringMetadataBuilder()
                        .AtLocus(Locus.B)
                        .Build()));
        }

        public abstract void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType);
    }
}