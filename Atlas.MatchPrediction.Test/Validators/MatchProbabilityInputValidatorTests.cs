using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
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
            var input = MatchProbabilityInputBuilder.New.Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenPatientHlaNotProvided_ValidationFails()
        {
            var input = MatchProbabilityInputBuilder.New.WithPatientHla(null).Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenDonorHlaNotProvided_ValidationFails()
        {
            var input = MatchProbabilityInputBuilder.New.WithDonorHla(null).Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenHlaNomenclatureNotProvided_ValidationFails()
        {
            var input = MatchProbabilityInputBuilder.New.WithHlaNomenclature(null).Build();

            var result = new MatchProbabilityInputValidator().Validate(input);

            result.IsValid.Should().BeFalse();
        }
    }
}