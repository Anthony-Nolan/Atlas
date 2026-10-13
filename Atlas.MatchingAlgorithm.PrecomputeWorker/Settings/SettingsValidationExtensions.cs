using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Options;
using MessagingServiceBusSettings = Atlas.MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Settings;

/// <summary>
/// The settings of the worker, validated when the host starts: a worker with a bad setting stops at once, rather than
/// fail every batch that it takes. The same shape as the settings of the match prediction worker.
/// </summary>
internal static class SettingsValidationExtensions
{
    public static void AddWorkerValidatedOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApplicationInsightsSettings>(configuration, "ApplicationInsights");
        services.AddValidatedOptions<HlaMetadataDictionarySettings, HlaMetadataDictionarySettingsValidator>(configuration, "HlaMetadataDictionary");
        services.AddOptions<HaplotypeFrequencySetCacheSettings>(configuration, "HaplotypeFrequencySetCache");
        services.AddOptions<GenotypeImputationSettings>(configuration, "GenotypeImputation");
        services.AddValidatedOptions<MacDictionarySettings, MacDictionarySettingsValidator>(configuration, "MacDictionary");
        services.AddValidatedOptions<MessagingServiceBusSettings, MessagingServiceBusSettingsValidator>(configuration, "MessagingServiceBus");
        services.AddOptions<DonorGenotypePrecomputationWorkerSettings>(configuration, "PrecomputeWorker");
    }

    private static void AddOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddValidatedOptions<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();
        services.AddOptions<TOptions>(configuration, sectionName);
    }
}

internal static class SettingsValidationHelpers
{
    public static ValidateOptionsResult SuccessOrFailures(List<string> failures)
    {
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    public static bool IsPlaceholder(string? value)
    {
        return string.Equals(value, "override-this", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class HlaMetadataDictionarySettingsValidator : IValidateOptions<HlaMetadataDictionarySettings>
{
    public ValidateOptionsResult Validate(string? name, HlaMetadataDictionarySettings options)
    {
        var failures = new List<string>();

        if (SettingsValidationHelpers.IsPlaceholder(options.AzureStorageConnectionString))
        {
            failures.Add("HlaMetadataDictionary:AzureStorageConnectionString must be replaced with a real Azure Storage connection string.");
        }

        if (options.DelayMilliseconds > options.MaxDelayMilliseconds)
        {
            failures.Add("HlaMetadataDictionary:DelayMilliseconds must be less than or equal to MaxDelayMilliseconds.");
        }

        return SettingsValidationHelpers.SuccessOrFailures(failures);
    }
}

internal sealed class MacDictionarySettingsValidator : IValidateOptions<MacDictionarySettings>
{
    public ValidateOptionsResult Validate(string? name, MacDictionarySettings options)
    {
        var failures = new List<string>();

        if (SettingsValidationHelpers.IsPlaceholder(options.AzureStorageConnectionString))
        {
            failures.Add("MacDictionary:AzureStorageConnectionString must be replaced with a real Azure Storage connection string.");
        }

        return SettingsValidationHelpers.SuccessOrFailures(failures);
    }
}

internal sealed class MessagingServiceBusSettingsValidator : IValidateOptions<MessagingServiceBusSettings>
{
    public ValidateOptionsResult Validate(string? name, MessagingServiceBusSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString) || SettingsValidationHelpers.IsPlaceholder(options.ConnectionString))
        {
            failures.Add("MessagingServiceBus:ConnectionString must be a real Azure Service Bus connection string: the worker reads its batches there.");
        }

        return SettingsValidationHelpers.SuccessOrFailures(failures);
    }
}
