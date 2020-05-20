using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications.MessageModels;
using Newtonsoft.Json;
using System;

namespace Atlas.Common.Notifications
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