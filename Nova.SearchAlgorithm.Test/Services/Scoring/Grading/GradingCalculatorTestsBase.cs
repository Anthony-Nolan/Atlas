using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
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
        public void CalculateGrade_BothResultsAreNull_ReturnsPGroup()
        {
            var grade = GradingCalculator.CalculateGrade(null, null);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_PatientResultIsNull_ReturnsPGroup()
        {
            var grade = GradingCalculator.CalculateGrade(null, new HlaScoringLookupResultBuilder().Build());

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_DonorResultIsNull_ReturnsPGroup()
        {
            var grade = GradingCalculator.CalculateGrade(new HlaScoringLookupResultBuilder().Build(), null);

            grade.Should().Be(MatchGrade.PGroup);
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

        public abstract void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType);
    }
}