using Atlas.Utils.Core.Attributes;

namespace Atlas.Utils.Notifications.MessageModels
{
    public abstract class BaseNotificationsMessage
    {
        public string Summary { get; }
        public string Description { get; }
        public string Originator { get; }

        protected BaseNotificationsMessage(string summary, string description, string originator)
        {
            Summary = summary;
            Description = description;
            Originator = originator ?? EntryAssemblyAttribute.GetReducedEntryAssembly();
        }
    }
}