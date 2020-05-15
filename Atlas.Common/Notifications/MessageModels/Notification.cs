namespace Atlas.Common.Notifications.MessageModels
{
    public class Notification : BaseNotificationsMessage
    {
        public Notification(string summary, string description, string originator = null) : base(summary, description, originator)
        {
        }
    }
}