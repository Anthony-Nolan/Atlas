using System.Reflection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.SearchTracking.Common.ApplicationInsights;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
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
    /// <param name="Configure">The app's <c>Startup.Configure</c>: all that its <c>Program</c> registers, apart from the host and Application Insights.</param>
    public record FunctionApp(string Name, Action<IServiceCollection> Configure)
    {
        public Assembly Assembly => Configure.Method.DeclaringType!.Assembly;

        public override string ToString() => Name;
    }

    private static IEnumerable<TestCaseData> FunctionApps()
    {
        return new[]
        {
            new FunctionApp("DonorImport", global::Atlas.DonorImport.Functions.Startup.Configure),
            new FunctionApp("Functions", global::Atlas.Functions.Startup.Configure),
            new FunctionApp("PublicApi", global::Atlas.Functions.PublicApi.Startup.Configure),
            new FunctionApp("MatchPrediction", global::Atlas.MatchPrediction.Functions.Startup.Configure),
            new FunctionApp("MatchingAlgorithm", global::Atlas.MatchingAlgorithm.Functions.Startup.Configure),
            new FunctionApp("DataRefresh", global::Atlas.MatchingAlgorithm.Functions.DataRefresh.Startup.Configure),
            new FunctionApp("DonorManagement", global::Atlas.MatchingAlgorithm.Functions.DonorManagement.Startup.Configure),
            new FunctionApp("RepeatSearch", global::Atlas.RepeatSearch.Functions.Startup.Configure),
            new FunctionApp("SearchTracking", global::Atlas.SearchTracking.Functions.Startup.Configure),
        }.Select(app => new TestCaseData(app).SetName($"{{m}}({app.Name})"));
    }

    /// <summary>The apps that load haplotype frequency sets.</summary>
    private static IEnumerable<TestCaseData> FrequencySetApps() =>
        FunctionApps().Where(testCase => testCase.Arguments[0] is FunctionApp { Name: "Functions" or "MatchPrediction" or "DataRefresh" or "DonorManagement" });

    /// <summary>The apps that send search tracking events to Application Insights.</summary>
    private static IEnumerable<TestCaseData> SearchTrackingApps() =>
        FunctionApps().Where(testCase => testCase.Arguments[0] is FunctionApp { Name: "PublicApi" or "MatchingAlgorithm" or "RepeatSearch" });

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

    /// <summary>
    /// The provider does not look inside registration factories when it is built. So a factory that resolves a missing
    /// registration (such as a keyed Service Bus client), or a singleton factory that resolves a scoped service (a captive
    /// dependency), only fails when the service is resolved. This resolves every Atlas factory registration in effect, and
    /// reports only those failures: others, such as the connection strings this host does not have, are not what it checks.
    /// </summary>
    [TestCaseSource(nameof(FunctionApps))]
    public async Task FactoryRegistrations_CanBeResolved(FunctionApp app)
    {
        using var host = BuildHost(app, out var services);
        await using var scope = host.Services.CreateAsyncScope();

        var failures = services
            .Where(descriptor => !descriptor.ServiceType.ContainsGenericParameters)
            .GroupBy(descriptor => (descriptor.ServiceType, descriptor.ServiceKey))
            .Select(registrations => registrations.Last())
            .Where(IsAtlasFactory)
            .Select(descriptor => (Service: Describe(descriptor), Error: DependencyInjectionError(() => Resolve(scope.ServiceProvider, descriptor))))
            .Where(result => result.Error != null)
            .Select(result => $"{result.Service}: {result.Error}")
            .ToList();

        failures.Should().BeEmpty();
    }

    /// <summary>
    /// The host runs the <see cref="IStartupValidator"/> when it starts, before any function: options registered with
    /// <c>ValidateOnStart</c> stop the app there.
    /// </summary>
    [TestCaseSource(nameof(FunctionApps))]
    public void StartupValidation_WithTheTemplateSettings_Passes(FunctionApp app)
    {
        using var host = BuildHost(app);

        var validate = () => host.Services.GetService<IStartupValidator>()?.Validate();

        validate.Should().NotThrow();
    }

    /// <summary>
    /// An older <c>local.settings.json</c> has <c>MaxCachedFrequencySets</c>, which the memory limit replaced: without a
    /// check, the limit would be 0, and each process would keep one set at a time.
    /// </summary>
    [TestCaseSource(nameof(FrequencySetApps))]
    public void StartupValidation_WithoutTheFrequencySetMemoryLimit_Fails(FunctionApp app)
    {
        using var host = BuildHost(app, settings => settings.Remove("HaplotypeFrequencySetCache:MaxCachedFrequencySetMegabytes"));

        var validate = () => host.Services.GetRequiredService<IStartupValidator>().Validate();

        validate.Should().Throw<OptionsValidationException>()
            .Which.OptionsType.Should().Be<HaplotypeFrequencySetCacheSettings>();
    }

    /// <summary>
    /// Without the initializer, sampling can drop the search tracking events that the app sends, and nothing else shows it.
    /// </summary>
    [TestCaseSource(nameof(SearchTrackingApps))]
    public void TelemetryConfiguration_OfSearchTrackingApp_HasTheSearchTrackingEventInitializer(FunctionApp app)
    {
        using var host = BuildHost(app);

        var initializers = host.Services.GetRequiredService<TelemetryConfiguration>().TelemetryInitializers;

        initializers.Should().ContainSingle(initializer => initializer is SearchTrackingEventTelemetryInitializer);
    }

    private static IHost BuildHost(FunctionApp app, Action<Dictionary<string, string?>>? changeSettings = null) =>
        BuildHost(app, out _, changeSettings);

    private static IHost BuildHost(FunctionApp app, out IServiceCollection services, Action<Dictionary<string, string?>>? changeSettings = null)
    {
        IServiceCollection? registered = null;
        var settings = TemplateSettings(app);
        changeSettings?.Invoke(settings);

        var host = new HostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(settings))
            .ConfigureFunctionsWebApplication(_ => { })
            .ConfigureServices(collection =>
            {
                collection.AddApplicationInsightsTelemetryWorkerService();
                collection.ConfigureFunctionsApplicationInsights();

                app.Configure(collection);
                registered = collection;
            })
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .Build();

        services = registered!;
        return host;
    }

    /// <summary>
    /// The app's <c>local.settings.template.json</c>, so that factories get past reading their settings to the services
    /// they resolve. Placeholder values are replaced with fakes that parse but are never connected to.
    /// </summary>
    private static Dictionary<string, string?> TemplateSettings(FunctionApp app)
    {
        var templatePath = Path.Combine(RepositoryRoot(), app.Assembly.GetName().Name!, "local.settings.template.json");
        var template = JObject.Parse(File.ReadAllText(templatePath));

        var values = ((JObject?)template["Values"])?.Properties() ?? [];
        var connectionStrings = ((JObject?)template["ConnectionStrings"])?.Properties()
            .Select(property => new JProperty($"ConnectionStrings:{property.Name}", property.Value)) ?? [];

        return values.Concat(connectionStrings).ToDictionary(
            property => property.Name,
            property => (string?)FakeIfPlaceholder(property.Name, property.Value.ToString()));
    }

    private static string FakeIfPlaceholder(string key, string value)
    {
        if (value != "override-this")
        {
            return value;
        }

        return key switch
        {
            _ when key.Contains("ServiceBus") && key.EndsWith("ConnectionString") =>
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==",
            _ when key.Contains("AppConfiguration") => "Endpoint=https://test.azconfig.io;Id=test;Secret=dGVzdA==",
            _ when key.EndsWith("Url") => "https://localhost/",
            _ => "test",
        };
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Atlas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root (Atlas.slnx).");
    }

    private static bool IsAtlasFactory(ServiceDescriptor descriptor)
    {
        var factory = descriptor.IsKeyedService ? descriptor.KeyedImplementationFactory?.Method : descriptor.ImplementationFactory?.Method;
        return factory?.DeclaringType?.Namespace?.StartsWith("Atlas") == true;
    }

    private static string Describe(ServiceDescriptor descriptor) =>
        descriptor.IsKeyedService ? $"{descriptor.ServiceType.Name} (key {descriptor.ServiceKey})" : descriptor.ServiceType.Name;

    private static object? Resolve(IServiceProvider provider, ServiceDescriptor descriptor) =>
        descriptor.IsKeyedService
            ? provider.GetRequiredKeyedService(descriptor.ServiceType, descriptor.ServiceKey)
            : provider.GetService(descriptor.ServiceType);

    /// <summary>The message of a registration problem behind the failure to resolve, or null if there is none.</summary>
    private static string? DependencyInjectionError(Action resolve)
    {
        string[] registrationProblems =
        [
            "No service for type",
            "No keyed service for type",
            "Unable to resolve service for type",
            "from root provider",
            "Cannot consume scoped service",
        ];

        try
        {
            resolve();
            return null;
        }
        catch (Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is InvalidOperationException && registrationProblems.Any(current.Message.Contains))
                {
                    return current.Message;
                }
            }

            return null;
        }
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
