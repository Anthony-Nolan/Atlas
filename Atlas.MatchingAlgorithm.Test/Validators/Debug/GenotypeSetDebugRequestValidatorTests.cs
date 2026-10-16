using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Validators.Debug;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.Debug;

[TestFixture]
internal class GenotypeSetDebugRequestValidatorTests
{
    private Fixture fixture;
    private GenotypeSetDebugRequestValidator validator;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        validator = new GenotypeSetDebugRequestValidator();
    }

    [Test]
    public void Validate_WithATypingOnly_IsValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { HlaTyping = Typing() });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_WithDonorCodesOnly_IsValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { ExternalDonorCodes = fixture.CreateMany<string>(3).ToList() });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_WithNeitherATypingNorDonorCodes_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { ExternalDonorCodes = [] });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.HlaTyping));
    }

    [Test]
    public void Validate_WithATypingAndDonorCodes_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            ExternalDonorCodes = fixture.CreateMany<string>(1).ToList()
        });

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_WithMoreDonorCodesThanTheLimit_IsNotValid()
    {
        var codes = fixture.CreateMany<string>(GenotypeSetDebugRequestValidator.MaxExternalDonorCodes + 1).ToList();

        var result = validator.Validate(new GenotypeSetDebugRequest { ExternalDonorCodes = codes });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.ExternalDonorCodes));
    }

    [Test]
    public void Validate_WithRepeatedDonorCodes_CountsEachCodeOnce()
    {
        var codes = fixture.CreateMany<string>(GenotypeSetDebugRequestValidator.MaxExternalDonorCodes).ToList();

        var result = validator.Validate(new GenotypeSetDebugRequest { ExternalDonorCodes = codes.Concat(codes).ToList() });

        result.IsValid.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase(null)]
    public void Validate_WithAnEmptyDonorCode_IsNotValid(string emptyCode)
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { ExternalDonorCodes = [fixture.Create<string>(), emptyCode] });

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_WithFrequencySetMetadataForDonors_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = fixture.CreateMany<string>(1).ToList(),
            FrequencySetMetadata = fixture.Create<FrequencySetMetadata>()
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.FrequencySetMetadata));
    }

    [Test]
    public void Validate_WithFrequencySetMetadataAndAFrequencySetId_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            FrequencySetMetadata = fixture.Create<FrequencySetMetadata>(),
            HaplotypeFrequencySetId = fixture.Create<int>()
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.FrequencySetMetadata));
    }

    [Test]
    public void Validate_WithAFrequencySetIdForDonors_IsValid()
    {
        // How a tester stores a donor's rows against an older, inactive set.
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = fixture.CreateMany<string>(1).ToList(),
            HaplotypeFrequencySetId = fixture.Create<int>()
        });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_WithAnUndefinedAllowedLociKey_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1, (AllowedLociKey)99]
        });

        result.IsValid.Should().BeFalse();
    }

    private PhenotypeInfo<string> Typing() => new(fixture.Create<string>());
}
