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

    [Test]
    public void Validate_WithCompareWithStored_IsValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { HlaTyping = Typing(), CompareWithStored = true });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_WithCompareWithStoredAndSave_IsNotValid()
    {
        // A save changes the rows that the comparison reads.
        var result = validator.Validate(new GenotypeSetDebugRequest { HlaTyping = Typing(), CompareWithStored = true, Save = true });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.CompareWithStored));
    }

    [Test]
    public void Validate_WithTheDormantDatabase_IsValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = fixture.CreateMany<string>(1).ToList(),
            Database = TransientDatabaseRole.Dormant,
            CompareWithStored = true
        });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_WithTheDormantDatabaseAndSave_IsNotValid()
    {
        // Data Refresh writes to the dormant database.
        var result = validator.Validate(new GenotypeSetDebugRequest { HlaTyping = Typing(), Database = TransientDatabaseRole.Dormant, Save = true });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.Database));
    }

    [Test]
    public void Validate_WithAnUndefinedDatabase_IsNotValid()
    {
        var result = validator.Validate(new GenotypeSetDebugRequest { HlaTyping = Typing(), Database = (TransientDatabaseRole)99 });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.Database));
    }

    [TestCase(1)]
    [TestCase(GenotypeSetDebugRequestValidator.MaxDifferencesPerRowLimit)]
    public void Validate_WithMaxDifferencesPerRowInTheRange_IsValid(int maxDifferencesPerRow)
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            CompareWithStored = true,
            MaxDifferencesPerRow = maxDifferencesPerRow
        });

        result.IsValid.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(GenotypeSetDebugRequestValidator.MaxDifferencesPerRowLimit + 1)]
    public void Validate_WithMaxDifferencesPerRowOutsideTheRange_IsNotValid(int maxDifferencesPerRow)
    {
        var result = validator.Validate(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            CompareWithStored = true,
            MaxDifferencesPerRow = maxDifferencesPerRow
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(GenotypeSetDebugRequest.MaxDifferencesPerRow));
    }

    [Test]
    public void Validate_WithTheDefaults_ReadsTheActiveDatabase_AndIsValid()
    {
        var request = new GenotypeSetDebugRequest { HlaTyping = Typing() };

        request.Database.Should().Be(TransientDatabaseRole.Active);
        request.MaxDifferencesPerRow.Should().Be(GenotypeSetDebugRequest.DefaultMaxDifferencesPerRow);
        validator.Validate(request).IsValid.Should().BeTrue();
    }

    private PhenotypeInfo<string> Typing() => new(fixture.Create<string>());
}
