using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using NUnit.Framework;
using System;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    public abstract class GradingCalculatorTestsBase<TCalculator> 
        where TCalculator : IGradingCalculator, new()
    {
        protected IGradingCalculator GradingCalculator;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            GradingCalculator = new TCalculator();
        }

        [Test]
        public void CalculateGrade_BothResultsAreNull_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(null, null));
        }

        [Test]
        public void CalculateGrade_PatientResultIsNull_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(null, new HlaScoringLookupResultBuilder().Build()));
        }

        [Test]
        public void CalculateGrade_DonorResultIsNull_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(new HlaScoringLookupResultBuilder().Build(), null));
        }

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

        [Test]
        public abstract void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType);
    }
}