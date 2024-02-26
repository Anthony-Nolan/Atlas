using System;
using Atlas.Common.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.Common.Debugging
{
    public static class DebugLoggerRegistration
    {
        public static void RegisterDebugLogger(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchInsightsSettings)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.MakeSettingsAvailableForUse(fetchInsightsSettings);
            services.TryAddScoped<IDebugLogger, DebugLogger>();
        }
    }
}