using Azure.Messaging.ServiceBus;
using System.Collections.Generic;

namespace Atlas.Common.ServiceBus
{
    public static class ServiceBusMessageExtensions
    {
        /// <summary>
        /// Stamps the given values as Service Bus <see cref="ServiceBusMessage.ApplicationProperties"/>.
        /// Values that are <c>null</c> are not stamped, so optional ids do not show up as empty properties.
        /// </summary>
        public static void AddApplicationProperties(this ServiceBusMessage message, IReadOnlyDictionary<string, object> properties)
        {
            if (properties == null)
            {
                return;
            }

            foreach (var (key, value) in properties)
            {
                if (value != null)
                {
                    message.ApplicationProperties[key] = value;
                }
            }
        }
    }
}
