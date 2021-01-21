using System;
using System.Collections.Generic;
using System.Text;

namespace Atlas.Client.Models.Search.Requests
{
    public class RepeatSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        /// <summary>
        /// New donors added and updated donors after this date will be considered in this repeat search
        /// Normally the cutoff date would be the date of the previous search
        /// </summary>
        public DateTime SearchCutoffDate { get; set; }
        public string OriginalSearchId { get; set; }
    }
}
