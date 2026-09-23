using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
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
        // Before any trigger binding resolves: this instance needs a subscription of its own on the HLA Metadata
        // Dictionary update topic, so that it drops its cached copy when the dictionary is recreated rather than
        // leaving one arbitrary instance of this app to receive the notification on everyone's behalf.
        builder.AddPerInstanceHlaMetadataDictionaryCacheInvalidationSubscription();
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

        // Workaround to set Json.NET json serializer. Registration above is also required to make Json.Net work in non-http triggered functions
        builder.Services.AddMvcCore().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });
    })
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();


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
