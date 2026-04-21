using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;

internal class HlaMetadataDictionaryRegistrationTests
{
    [Test]
    public void RegisterFileBasedHlaMetadataDictionaryForTesting_AllowsServiceProviderValidationOnBuild()
    {
        var services = new ServiceCollection();

        services.RegisterFileBasedHlaMetadataDictionaryForTesting(
            _ => new ApplicationInsightsSettings { LogLevel = "Info" },
            _ => new MacDictionarySettings());

        var buildProvider = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        buildProvider.Should().NotThrow();
    }
}