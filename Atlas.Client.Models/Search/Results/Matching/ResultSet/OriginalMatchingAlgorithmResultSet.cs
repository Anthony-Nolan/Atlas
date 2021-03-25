namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public class OriginalMatchingAlgorithmResultSet : ResultSet<MatchingAlgorithmResult>
    {
        public override bool IsRepeatSearchSet => false;

        public override string ResultsFileName => $"{SearchRequestId}.json";
    }
}