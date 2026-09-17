using Atlas.MatchingAlgorithm.Functions.DataRefresh;
using Azure.Core.Serialization;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.Services.Configure<WorkerOptions>(options =>
        {
            var settings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.NullValueHandling = NullValueHandling.Ignore;

            options.Serializer = new NewtonsoftJsonObjectSerializer();
        });

        // Workaround to set Json.NET json serializer. Registration above is also required to make Json.Net work in non-http triggered functions
        builder.Services.AddMvcCore().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });
    })
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            // This worker writes telemetry DIRECTLY to Application Insights, so host.json's
            // samplingSettings.excludedTypes does NOT apply to it - that only governs the Functions HOST pipeline.
            // Disable the SDK's default adaptive sampling here so we can re-add it below with excludedTypes, keeping
            // diagnostic Traces/Exceptions/Events off the sampling table (mirroring host.json's stated intent) while
            // still sampling the very-high-volume SQL Dependency telemetry emitted during a Data Refresh.
            // NB: Data Refresh timings no longer rely on this - they are emitted as (never-sampled) metrics - but the
            // run manifest Event, the progress/ETA and error Traces, and the bulk-write session's reuse count are all
            // sampled without it. Those are what say whether a run is measurable at all, so they must be retained.
            options.EnableAdaptiveSampling = false;
        });
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
        {
            telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
                .UseAdaptiveSampling(maxTelemetryItemsPerSecond: 20, excludedTypes: "Trace;Exception;Event")
                .Build();
        });

        Startup.Configure(services);
    })
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

host.Run();
