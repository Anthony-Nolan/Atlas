namespace Atlas.Utils.Notifications
{
    public class Alert : BaseNotificationsMessage
    {
        public Priority Priority { get; }

        public Alert(string summary, string description, Priority priority, string originator = null)
            : base(summary, description, originator)
        {
            Priority = priority;
        }
    }
}