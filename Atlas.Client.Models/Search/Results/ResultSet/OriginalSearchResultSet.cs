namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public class OriginalSearchResultSet : SearchResultSet
    {
        public override bool IsRepeatSearchSet => false;
      
        public override string ResultsFileName => $"{SearchRequestId}.json";
    }
}