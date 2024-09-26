using Atlas.Common.Utils.Extensions;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Services;
using Microsoft.EntityFrameworkCore;
using Atlas.SearchTracking.Common.Settings.ServiceBus;

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

    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContext<ISearchTrackingContext, SearchTrackingContext>(options =>
        {
            options.UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings:PersistentSql"));
        });

        services.AddScoped<ISearchTrackingEventProcessor, SearchTrackingEventProcessor>();
        services.AddScoped<ISearchRequestRepository, SearchRequestRepository>();
        services.AddScoped<IMatchPredictionRepository, MatchPredictionRepository>();
        services.AddScoped<ISearchRequestMatchingAlgorithmAttemptsRepository, SearchRequestMatchingAlgorithmAttemptsRepository>();
        services.RegisterAsOptions<SearchTrackingServiceBusSettings>("MessagingServiceBus");
    })
    .Build();

host.Run();
