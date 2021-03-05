namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public class OriginalMatchingAlgorithmResultSet : MatchingAlgorithmResultSet
    {
        public override bool IsRepeatSearchSet => false;

        public override string ResultsFileName => $"{SearchRequestId}.json";
    }
}