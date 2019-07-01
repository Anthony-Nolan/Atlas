using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using NUnit.Framework;
using System;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
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
                    new HlaScoringLookupResultBuilder()
                        .AtLocus(Locus.A)
                        .Build(),
                    new HlaScoringLookupResultBuilder()
                        .AtLocus(Locus.B)
                        .Build()));
        }

        public abstract void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType);
    }
}