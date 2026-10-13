using System.Reflection;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Atlas.DependencyInjection.Test;

/// <summary>
/// Builds the service provider of each functions app the way its <c>Program</c> does, so that a missing registration
/// fails here rather than when the app starts. The compiler cannot catch one: a registration may be needed by a class
/// that only the runtime resolves, such as a keyed Service Bus client injected with <see cref="FromKeyedServicesAttribute"/>.
/// </summary>
[TestFixture]
internal class FunctionAppDependencyInjectionTests
{
    /// <param name="Configure">What the app's <c>Program</c> registers, apart from the host and Application Insights.</param>
    public record FunctionApp(string Name, Assembly Assembly, params Action<IServiceCollection>[] Configure)
    {
        public override string ToString() => Name;
    }

    private static IEnumerable<TestCaseData> FunctionApps()
    {
        return new[]
        {
            new FunctionApp("DonorImport", typeof(global::Atlas.DonorImport.Functions.Startup).Assembly,
                global::Atlas.DonorImport.Functions.Startup.Configure, global::Atlas.DonorImport.Functions.SwashBuckleStartup.Configure),
            new FunctionApp("Functions", typeof(global::Atlas.Functions.Startup).Assembly,
                global::Atlas.Functions.Startup.Configure, global::Atlas.Functions.SwashBuckleStartup.Configure),
            new FunctionApp("PublicApi", typeof(global::Atlas.Functions.PublicApi.Startup).Assembly,
                global::Atlas.Functions.PublicApi.Startup.Configure, global::Atlas.Functions.PublicApi.SwashBuckleStartup.Configure),
            new FunctionApp("MatchPrediction", typeof(global::Atlas.MatchPrediction.Functions.Startup).Assembly,
                global::Atlas.MatchPrediction.Functions.Startup.Configure, global::Atlas.MatchPrediction.Functions.SwashBuckleStartup.Configure),
            new FunctionApp("MatchingAlgorithm", typeof(global::Atlas.MatchingAlgorithm.Functions.Startup).Assembly,
                global::Atlas.MatchingAlgorithm.Functions.Startup.Configure, global::Atlas.MatchingAlgorithm.Functions.SwashBuckleStartup.Configure),
            new FunctionApp("DataRefresh", typeof(global::Atlas.MatchingAlgorithm.Functions.DataRefresh.Startup).Assembly,
                global::Atlas.MatchingAlgorithm.Functions.DataRefresh.Startup.Configure),
            new FunctionApp("DonorManagement", typeof(global::Atlas.MatchingAlgorithm.Functions.DonorManagement.Startup).Assembly,
                global::Atlas.MatchingAlgorithm.Functions.DonorManagement.Startup.Configure),
            new FunctionApp("RepeatSearch", typeof(global::Atlas.RepeatSearch.Functions.Startup).Assembly,
                global::Atlas.RepeatSearch.Functions.Startup.Configure, global::Atlas.RepeatSearch.Functions.SwashBuckleStartup.Configure),
            new FunctionApp("SearchTracking", typeof(global::Atlas.SearchTracking.Functions.Startup).Assembly,
                global::Atlas.SearchTracking.Functions.Startup.Configure),
        }.Select(app => new TestCaseData(app).SetName($"{{m}}({app.Name})"));
    }

    [TestCaseSource(nameof(FunctionApps))]
    public void ServiceProvider_IsValid(FunctionApp app)
    {
        var build = () => BuildHost(app).Dispose();

        build.Should().NotThrow();
    }

    /// <summary>
    /// The functions runtime, not the container, creates the function classes, so the provider does not validate them
    /// when it is built.
    /// </summary>
    [TestCaseSource(nameof(FunctionApps))]
    public void FunctionClasses_HaveResolvableDependencies(FunctionApp app)
    {
        using var host = BuildHost(app);
        var isService = host.Services.GetRequiredService<IServiceProviderIsKeyedService>();

        var functionClasses = app.Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetMethods().Any(method => method.GetCustomAttribute<FunctionAttribute>() != null))
            .ToList();

        var unresolvable = functionClasses
            .Where(type => !type.GetConstructors().Any(constructor => constructor.GetParameters().All(parameter => CanResolve(parameter, isService))))
            .Select(type => $"{type.Name}: {string.Join(", ", UnresolvableParameters(type, isService))}");

        functionClasses.Should().NotBeEmpty();
        unresolvable.Should().BeEmpty();
    }

    private static IHost BuildHost(FunctionApp app)
    {
        return new HostBuilder()
            .ConfigureFunctionsWebApplication(_ => { })
            .ConfigureServices(services =>
            {
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();

                foreach (var configure in app.Configure)
                {
                    configure(services);
                }
            })
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .Build();
    }

    private static bool CanResolve(ParameterInfo parameter, IServiceProviderIsKeyedService isService)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        var keyed = parameter.GetCustomAttribute<FromKeyedServicesAttribute>();
        return keyed == null
            ? isService.IsService(parameter.ParameterType)
            : isService.IsKeyedService(parameter.ParameterType, keyed.Key);
    }

    private static IEnumerable<string> UnresolvableParameters(Type type, IServiceProviderIsKeyedService isService)
    {
        return type.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Where(parameter => !CanResolve(parameter, isService))
            .Select(parameter => parameter.ParameterType.Name)
            .Distinct();
    }
}
