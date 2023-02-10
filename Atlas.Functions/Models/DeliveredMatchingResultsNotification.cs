using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Functions.Models
{
    /// <summary>
    /// <see cref="MatchingResultsNotification"/> extended with metadata concerning the notification message and its delivery.
    /// </summary>
    public class DeliveredMatchingResultsNotification : MatchingResultsNotification
    {
        /// <summary>
        /// How many times the notification has been delivered so far; includes the current delivery,
        /// i.e., first delivery will have <see cref="MessageDeliveryCount"/> of 1.
        /// </summary>
        public int MessageDeliveryCount { get; set; }
    }
}
