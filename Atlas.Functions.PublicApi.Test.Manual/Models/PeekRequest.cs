namespace Atlas.Functions.PublicApi.Test.Manual.Models
{
    public class PeekRequest
    {
        public long FromSequenceNumber { get; set; }
        public int MessageCount { get; set; }
    }

    public class PeekBySearchRequestIdRequest : PeekRequest
    {
        public string SearchRequestId { get; set; }
    }
}