using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Client.Models.Search.Requests;

namespace Atlas.RepeatSearch.Models
{
    public class IdentifiedRepeatSearchRequest
    {
        public RepeatSearchRequest RepeatSearchRequest { get; set; }
        public string RepeatSearchID { get; set; }
    }
}
