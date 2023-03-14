using Atlas.Client.Models.Search.Results.ResultSet;

namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public class OriginalMatchingAlgorithmResultSet : BatchedResultSet<MatchingAlgorithmResult>
    {
        public override bool IsRepeatSearchSet => false;

        public override string ResultsFileName => $"{SearchRequestId}.json";
    }
}