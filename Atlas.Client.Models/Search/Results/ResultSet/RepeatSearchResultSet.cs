using System.Collections.Generic;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public class RepeatSearchResultSet : SearchResultSet
    {
        public string RepeatSearchId { get; set; }
        public override bool IsRepeatSearchSet => true;
        public override string ResultsFileName => $"{SearchRequestId}/{RepeatSearchId}.json";
        
        public IEnumerable<string> NoLongerMatchingDonorCodes { get; set; } 
    }
}