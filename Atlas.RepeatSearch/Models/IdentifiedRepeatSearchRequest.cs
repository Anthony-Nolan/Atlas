using System;
using Atlas.Client.Models.Search.Requests;

namespace Atlas.RepeatSearch.Models
{
    public class IdentifiedRepeatSearchRequest
    {
        public RepeatSearchRequest RepeatSearchRequest { get; set; }
        public string RepeatSearchId { get; set; }
        public string OriginalSearchId { get; set; }

        /// <summary>
        /// Time of the first request message, set when the request is published again to wait for the result set of the original search.
        /// Null on the first message: its enqueued time is the time of the first request.
        /// </summary>
        public DateTimeOffset? FirstRequestTimeUtc { get; set; }
    }
}
