using System;
using System.Collections.Generic;
using System.Text;

namespace Atlas.Client.Models.Search.Requests
{
    public class RepeatSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        public DateTime SearchCutoffDate { get; set; }
        public string PreviousSearchId { get; set; }
    }
}
