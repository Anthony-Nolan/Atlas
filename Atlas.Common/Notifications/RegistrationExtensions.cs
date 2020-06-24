using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
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
            services.MakeOptionsAvailableForUse(fetchNotificationSettings);
            services.RegisterAtlasLogger(fetchInsightsSettings);

            services.AddScoped<INotificationsClient, NotificationsClient>();
            services.AddScoped<INotificationSender, NotificationSender>();
        }
    }
}
