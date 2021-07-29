using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using Atlas.MatchPrediction.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Validators
{
    [TestFixture]
    internal class MatchProbabilityInputValidatorTests
    {
        [Test]
        public void Validator_ForValidInput_ValidationPasses()
        {
            var input = SingleDonorMatchProbabilityInputBuilder.Default.Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenPatientHlaNotProvided_ValidationFails()
        {
            var input = SingleDonorMatchProbabilityInputBuilder.Default.WithPatientHla(null).Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenDonorHlaNotProvided_ValidationFails()
        {
            var input = SingleDonorMatchProbabilityInputBuilder.Default.WithDonorHla(null).Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }

        [TestCase(new []{ Locus.A})]
        [TestCase(new []{ Locus.B})]
        [TestCase(new []{ Locus.Drb1})]
        [TestCase(new []{ Locus.A, Locus.B})]
        [TestCase(new []{ Locus.A, Locus.B, Locus.Drb1})]
        [TestCase(new []{ Locus.A, Locus.C})]
        public void Validator_WhenRequiredLociExcluded_ValidationFails(Locus[] excludedLoci)
        {
            var input = SingleDonorMatchProbabilityInputBuilder.Default.WithExcludedLoci(excludedLoci).Build();
            
            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }
    }
}