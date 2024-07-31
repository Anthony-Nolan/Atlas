using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static void RegisterServiceBusServices(this IServiceCollection services, Func<IServiceProvider, string> connectionStringAccessor)
        {
            services.TryAddSingleton(isp => new ServiceBusClient(connectionStringAccessor(isp)));
            services.TryAddSingleton<IMessageReceiverFactory, MessageReceiverFactory>();
            services.TryAddSingleton<ITopicClientFactory, TopicClientFactory>();
        }

        public static void RegisterServiceBusAsKeyedServices(this IServiceCollection services, object key, Func<IServiceProvider, string> connectionStringAccessor)
        {
            services.TryAddKeyedSingleton(key, (isp, _) => new ServiceBusClient(connectionStringAccessor(isp)));
            services.TryAddKeyedSingleton<IMessageReceiverFactory>(key, (isp, key) => new MessageReceiverFactory(isp.GetRequiredKeyedService<ServiceBusClient>(key)));
            services.TryAddKeyedSingleton<ITopicClientFactory>(key, (isp, key) => new TopicClientFactory(isp.GetRequiredKeyedService<ServiceBusClient>(key)));
        }
    }
}
