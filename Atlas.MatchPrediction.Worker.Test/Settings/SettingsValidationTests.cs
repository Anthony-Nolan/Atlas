using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Worker.Test.Settings;

[TestFixture]
internal class SettingsValidationTests
{
    private const string Placeholder = "override-this";

    private Fixture fixture;

    [SetUp]
    public void SetUp() => fixture = new Fixture();

    [TestCase("override-this")]
    [TestCase("OVERRIDE-THIS")]
    [TestCase("Override-This")]
    public void IsPlaceholder_MatchesOverrideThisCaseInsensitively(string value)
    {
        SettingsValidationHelpers.IsPlaceholder(value).Should().BeTrue();
    }

    [Test]
    public void IsPlaceholder_ForRealValue_ReturnsFalse()
    {
        SettingsValidationHelpers.IsPlaceholder(fixture.Create<string>()).Should().BeFalse();
    }

    [Test]
    public void SuccessOrFailures_WhenNoFailures_ReturnsSuccess()
    {
        SettingsValidationHelpers.SuccessOrFailures([]).Succeeded.Should().BeTrue();
    }

    [Test]
    public void SuccessOrFailures_WhenFailures_ReturnsFail()
    {
        SettingsValidationHelpers.SuccessOrFailures([fixture.Create<string>()]).Failed.Should().BeTrue();
    }

    [Test]
    public void AzureStorageValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new AzureStorageSettingsValidator().Validate(null, new AzureStorageSettings { ConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void AzureStorageValidator_WhenConnectionStringIsReal_Succeeds()
    {
        var result = new AzureStorageSettingsValidator().Validate(null, new AzureStorageSettings { ConnectionString = fixture.Create<string>() });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void AzureStorageValidator_WhenConnectionStringIsBlank_Succeeds()
    {
        var result = new AzureStorageSettingsValidator().Validate(null, new AzureStorageSettings { ConnectionString = "  " });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void HlaMetadataDictionaryValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new HlaMetadataDictionarySettingsValidator().Validate(null,
            new HlaMetadataDictionarySettings { AzureStorageConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void HlaMetadataDictionaryValidator_WhenDelayExceedsMaxDelay_Fails()
    {
        var result = new HlaMetadataDictionarySettingsValidator().Validate(null,
            new HlaMetadataDictionarySettings
            {
                AzureStorageConnectionString = fixture.Create<string>(),
                DelayMilliseconds = 100,
                MaxDelayMilliseconds = 50,
            });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void HlaMetadataDictionaryValidator_WhenValid_Succeeds()
    {
        var result = new HlaMetadataDictionarySettingsValidator().Validate(null,
            new HlaMetadataDictionarySettings
            {
                AzureStorageConnectionString = fixture.Create<string>(),
                DelayMilliseconds = 50,
                MaxDelayMilliseconds = 100,
            });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void MacDictionaryValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new MacDictionarySettingsValidator().Validate(null,
            new MacDictionarySettings { AzureStorageConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void MacDictionaryValidator_WhenConnectionStringIsReal_Succeeds()
    {
        var result = new MacDictionarySettingsValidator().Validate(null,
            new MacDictionarySettings { AzureStorageConnectionString = fixture.Create<string>() });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void MessagingServiceBusValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new MessagingServiceBusSettingsValidator().Validate(null,
            new MessagingServiceBusSettings { ConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void MessagingServiceBusValidator_WhenConnectionStringIsReal_Succeeds()
    {
        var result = new MessagingServiceBusSettingsValidator().Validate(null,
            new MessagingServiceBusSettings { ConnectionString = fixture.Create<string>() });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void NotificationsServiceBusValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new NotificationsServiceBusSettingsValidator().Validate(null,
            new NotificationsServiceBusSettings { ConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void NotificationsServiceBusValidator_WhenConnectionStringIsReal_Succeeds()
    {
        var result = new NotificationsServiceBusSettingsValidator().Validate(null,
            new NotificationsServiceBusSettings { ConnectionString = fixture.Create<string>() });
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void SearchTrackingServiceBusValidator_WhenConnectionStringIsPlaceholder_Fails()
    {
        var result = new SearchTrackingServiceBusSettingsValidator().Validate(null,
            new SearchTrackingServiceBusSettings { ConnectionString = Placeholder });
        result.Failed.Should().BeTrue();
    }

    [Test]
    public void SearchTrackingServiceBusValidator_WhenConnectionStringIsReal_Succeeds()
    {
        var result = new SearchTrackingServiceBusSettingsValidator().Validate(null,
            new SearchTrackingServiceBusSettings { ConnectionString = fixture.Create<string>() });
        result.Succeeded.Should().BeTrue();
    }
}
