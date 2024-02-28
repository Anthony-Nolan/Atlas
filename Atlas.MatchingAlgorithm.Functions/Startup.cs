using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchingAlgorithm.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterSearchForMatchingAlgorithm(OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<MatchingConfigurationSettings>(),
                ConnectionStringReader("PersistentSql"),
                ConnectionStringReader("SqlA"), 
                ConnectionStringReader("SqlB"),
                ConnectionStringReader("DonorSql"));
            
            builder.Services.RegisterDataRefresh(OptionsReaderFor<AzureAuthenticationSettings>(),
                OptionsReaderFor<AzureDatabaseManagementSettings>(),
                OptionsReaderFor<DataRefreshSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<DonorManagementSettings>(),
                ConnectionStringReader("PersistentSql"),
                ConnectionStringReader("SqlA"),
                ConnectionStringReader("SqlB"), 
                ConnectionStringReader("DonorSql"));

            RegisterLogQueryClient(builder.Services, builder.GetContext().Configuration);

            builder.Services.RegisterDebugServices(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>());
        }

        /// <summary>
        /// Feature management, leave it configured even if there is no active feature flags in use
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var azureConfigurationConnectionString = Environment.GetEnvironmentVariable("AzureAppConfiguration:ConnectionString");
            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(azureConfigurationConnectionString)
                    .Select("_")
                    .UseFeatureFlags();
            });

            base.ConfigureAppConfiguration(builder);
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureAuthenticationSettings>("AzureManagement:Authentication");
            services.RegisterAsOptions<AzureDatabaseManagementSettings>("AzureManagement:Database");
            services.RegisterAsOptions<AzureMonitoringSettings>("AzureManagement:Monitoring");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DataRefreshSettings>("DataRefresh");
            services.RegisterAsOptions<DonorManagementSettings>("DataRefresh:DonorManagement");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MatchingConfigurationSettings>("MatchingConfiguration");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

        }

        private static void RegisterLogQueryClient(IServiceCollection services, IConfiguration config)
        {
            services.AddAzureClients(clientBuilder =>
            {
                var authSetting = config.GetSection("AzureManagement:Authentication").Get<AzureAuthenticationSettings>();
                clientBuilder.UseCredential(new ClientSecretCredential(
                    tenantId: authSetting.TenantId,
                    clientId: authSetting.ClientId,
                    clientSecret: authSetting.ClientSecret));

                clientBuilder.AddLogsQueryClient();
            });
        }

    }
}