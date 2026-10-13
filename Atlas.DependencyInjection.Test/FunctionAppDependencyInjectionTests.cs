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
    /// The provider does not look inside registration factories when it is built, so a singleton whose factory resolves a
    /// scoped service (a captive dependency) only fails when the singleton is resolved. Failures for any other reason, such
    /// as the connection strings this host does not have, are not what this test checks.
    /// </summary>
    [TestCaseSource(nameof(FunctionApps))]
    public void SingletonFactories_DoNotResolveScopedServices(FunctionApp app)
    {
        using var host = BuildHost(app, out var services);

        var captiveDependencies = services
            .Where(descriptor => !descriptor.IsKeyedService && !descriptor.ServiceType.ContainsGenericParameters)
            .GroupBy(descriptor => descriptor.ServiceType)
            .Select(registrations => registrations.Last())
            .Where(descriptor => descriptor is { Lifetime: ServiceLifetime.Singleton, ImplementationFactory: not null })
            .Where(descriptor => descriptor.ImplementationFactory!.Method.DeclaringType?.Namespace?.StartsWith("Atlas") == true)
            .Select(descriptor => (descriptor.ServiceType, Error: ScopedServiceResolvedFromRoot(host.Services, descriptor.ServiceType)))
            .Where(result => result.Error != null)
            .Select(result => $"{result.ServiceType.Name}: {result.Error}");

        captiveDependencies.Should().BeEmpty();
    }

    private static IHost BuildHost(FunctionApp app) => BuildHost(app, out _);

    private static IHost BuildHost(FunctionApp app, out IServiceCollection services)
    {
        IServiceCollection? registered = null;

        var host = new HostBuilder()
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

    private static string? ScopedServiceResolvedFromRoot(IServiceProvider root, Type serviceType)
    {
        try
        {
            root.GetService(serviceType);
            return null;
        }
        catch (Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is InvalidOperationException && current.Message.Contains("from root provider"))
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
