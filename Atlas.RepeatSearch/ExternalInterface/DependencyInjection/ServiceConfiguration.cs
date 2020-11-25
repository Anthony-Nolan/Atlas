using System;
using Atlas.Common.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.RepeatSearch.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterRepeatSearch(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> optionsReaderFor,
            Func<IServiceProvider, string> connectionStringReader)
        {
        }
    }
}