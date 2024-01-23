using System.Collections.Generic;
using Atlas.Client.Models.Debug;

namespace Atlas.ManualTesting.Models
{
    public class PeekBySearchRequestIdRequest : PeekServiceBusMessagesRequest
    {
        public string SearchRequestId { get; set; }
    }

    public class PeekByAtlasDonorIdsRequest : PeekServiceBusMessagesRequest
    {
        public IEnumerable<int> AtlasDonorIds { get; set; }
    }

    public class SearchOutcomesPeekRequest : PeekServiceBusMessagesRequest
    {
        /// <summary>
        /// Directory where files should be written to
        /// </summary>
        public string OutputDirectory { get; set; }
    }
}