using Atlas.MatchingAlgorithm.Functions;
using Azure.Core.Serialization;
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
    .ConfigureAppConfiguration(builder =>
    {
        var azureConfigurationConnectionString = Environment.GetEnvironmentVariable("AzureAppConfiguration:ConnectionString");

        builder.AddAzureAppConfiguration(options =>
        {
            options.Connect(azureConfigurationConnectionString)
                .Select("_")
                .UseFeatureFlags();
        });

    })
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.Services.Configure<WorkerOptions>(options =>
        {
            var settings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.NullValueHandling = NullValueHandling.Ignore;

            options.Serializer = new NewtonsoftJsonObjectSerializer();
        });
    })
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();


        Startup.Configure(services);
        SwashBuckleStartup.Configure(services);
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
