using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Functions;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;


namespace Atlas.DonorImport.Functions
{
    internal class Startup 
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.AddHealthChecks();

            services.RegisterDonorImport(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<NotificationConfigurationSettings>(),
                OptionsReaderFor<NotificationsServiceBusSettings>(),
                OptionsReaderFor<DonorImportSettings>(),
                OptionsReaderFor<PublishDonorUpdatesSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<FailureLogsSettings>(),
            ConnectionStringReader("DonorStoreSql")
            );

            services.RegisterDonorReader(ConnectionStringReader("DonorStoreSql"));
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<NotificationConfigurationSettings>("NotificationConfiguration");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");
            services.RegisterAsOptions<DonorImportSettings>("DonorImport");
            services.RegisterAsOptions<PublishDonorUpdatesSettings>("PublishDonorUpdates");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<FailureLogsSettings>("FailureLogs");
        }
    }
}