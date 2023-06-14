using System.Collections.Generic;

namespace Atlas.ManualTesting.Models
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

    public class PeekByAtlasDonorIdsRequest : PeekRequest
    {
        public IEnumerable<int> AtlasDonorIds { get; set; }
    }

    public class WmdaParallelSearchInfoPeekRequest : PeekRequest
    {
        public string Directory { get; set; }
    }
}