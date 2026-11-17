using Atlas.SearchTracking.Common.ApplicationInsights;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Atlas.DependencyInjection.Test;

/// <summary>
/// Builds the service provider of the match prediction worker the way its <c>Program</c> does, with the worker's <c>appsettings.json</c>.
/// The app is built, not started. The provider validates the registrations when it is built, so that a missing registration fails here
/// rather than when the worker starts.
/// </summary>
[TestFixture]
internal class MatchPredictionWorkerDependencyInjectionTests
{
    /// <summary>
    /// The provider checks that it can create each service that is registered with a type. It does not look inside registration factories.
    /// </summary>
    [Test]
    public async Task ServiceProvider_IsValid()
    {
        var build = async () => await BuildApp().DisposeAsync();

        await build.Should().NotThrowAsync();
    }

    /// <summary>
    /// Without the initializer, sampling can drop the search tracking events that the worker sends, and nothing else shows it.
    /// </summary>
    [Test]
    public async Task TelemetryConfiguration_HasTheSearchTrackingEventInitializer()
    {
        await using var app = BuildApp();

        var initializers = app.Services.GetRequiredService<TelemetryConfiguration>().TelemetryInitializers;

        initializers.Should().ContainSingle(initializer => initializer is SearchTrackingEventTelemetryInitializer);
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddJsonFile(Path.Combine(RepositoryPaths.Root(), "Atlas.MatchPrediction.Worker", "appsettings.json"), optional: false);

        global::Atlas.MatchPrediction.Worker.Startup.Configure(builder.Services, builder.Configuration);
        builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            }
        );

        return builder.Build();
    }
}