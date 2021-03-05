namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public class RepeatMatchingAlgorithmResultSet : MatchingAlgorithmResultSet
    {
        public string RepeatSearchId { get; set; }
        public override bool IsRepeatSearchSet => true;
        public override string ResultsFileName => $"{SearchRequestId}/{RepeatSearchId}.json";
    }
}