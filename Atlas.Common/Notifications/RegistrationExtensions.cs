using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
using Atlas.Common.Utils.Extensions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.Notifications
{
    public static class RegistrationExtensions
    {
        public static void RegisterNotificationSender(
            this IServiceCollection services,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationSettings,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchInsightsSettings)
        {
            var serviceKey = typeof(NotificationsServiceBusSettings);
            services.RegisterServiceBusAsKeyedServices(
                serviceKey ,
                isp => fetchNotificationSettings(isp).ConnectionString);

            services.MakeSettingsAvailableForUse(fetchNotificationSettings);
            services.RegisterAtlasLogger(fetchInsightsSettings);

            services.AddKeyedSingleton<ITopicClientFactory>(serviceKey, (sp, key) => new TopicClientFactory(sp.GetRequiredKeyedService<ServiceBusClient>(key)));
            services.AddScoped<INotificationsClient, NotificationsClient>();
            services.AddScoped<INotificationSender, NotificationSender>();
        }
    }
}
