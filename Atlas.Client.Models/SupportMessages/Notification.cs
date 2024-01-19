namespace Atlas.Client.Models.SupportMessages
{
    public class Notification : BaseNotificationsMessage
    {
        public Notification(string summary, string description, string originator = null) : base(summary, description, originator)
        {
        }
    }
}