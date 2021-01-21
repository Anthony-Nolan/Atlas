using System;
using System.Collections.Generic;
using System.Text;

namespace Atlas.RepeatSearch.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string RepeatSearchRequestsTopic { get; set; }
        public string RepeatSearchRequestsSubscription { get; set; }
        public string RepeatSearchResultsTopic { get; set; }
    }
}
