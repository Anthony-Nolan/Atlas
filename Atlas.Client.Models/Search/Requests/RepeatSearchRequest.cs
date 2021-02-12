using System;

namespace Atlas.Client.Models.Search.Requests
{
    public class RepeatSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        /// <summary>
        /// New donors added and updated donors after this date will be considered in this repeat search. (At a resolution of seconds)
        /// Normally the cutoff date would be the date of the previous search.
        /// </summary>
        public DateTimeOffset? SearchCutoffDate { get; set; }
        public string OriginalSearchId { get; set; }
    }
}
