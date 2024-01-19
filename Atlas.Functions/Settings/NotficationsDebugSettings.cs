namespace Atlas.Functions.Settings
{
    internal class NotificationsDebugSettings
    {
        /// <summary>
        /// Used by debug functions to peek messages from the alerts topic.
        /// </summary>
        public string AlertsSubscription { get; set; }

        /// <summary>
        /// Used by debug functions to peek messages from the notifications topic.
        /// </summary>
        public string NotificationsSubscription { get; set; }
    }
}
