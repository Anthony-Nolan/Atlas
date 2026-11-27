using System.Net.Http;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests.DependencyInjection;

[TestFixture]
internal class MacImportRegistrationTests
{
    [Test]
    public void RegisterMacImport_ResolvesMacCodeDownloader()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var downloader = scope.ServiceProvider.GetRequiredService<IMacCodeDownloader>();

        downloader.Should().BeOfType<MacCodeDownloader>();
    }

    [Test]
    public void RegisterMacImport_GivesMacFileClientTheDownloadTimeout()
    {
        using var provider = BuildProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(MacCodeDownloader.HttpClientName);

        client.Timeout.Should().Be(MacCodeDownloader.DownloadTimeout);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.RegisterMacImport(
            _ => new ApplicationInsightsSettings { LogLevel = "Info" },
            _ => new MacDictionarySettings(),
            _ => new MacDownloadSettings { MacSourceUrl = "https://example.org/mac.zip" });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
