using Newtonsoft.Json;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.ApplicationInsights.EventModels;
using Atlas.Utils.Notifications;
using System;

namespace Atlas.MatchingAlgorithm.ApplicationInsights
{
    public class NotificationSenderFailureEventModel : EventModel
    {
        private const string MessageName = "Error sending notification message";
        
        public NotificationSenderFailureEventModel(
            Exception exception,
            BaseNotificationsMessage message) : base(MessageName)
        {
            Level = LogLevel.Warn;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("Type", message.GetType().Name);
            Properties.Add("Message", JsonConvert.SerializeObject(message));
        }
    }
}