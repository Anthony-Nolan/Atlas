using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.ResultSet;

namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public class RepeatMatchingAlgorithmResultSet : ResultSet<MatchingAlgorithmResult>
    {
        public string RepeatSearchId { get; set; }
        public override bool IsRepeatSearchSet => true;
        public override string ResultsFileName => $"{SearchRequestId}/{RepeatSearchId}.json";
        
        /// <summary>
        /// External Donor Codes of all donors that were matching in the previous run of this search, and are no longer matching
        /// - whether they have been deleted or updated to no longer be matching. 
        /// </summary>
        public List<string> NoLongerMatchingDonors { get; set; }
    }
}