using Atlas.Client.Models.Search.Results;

namespace Atlas.Functions.Models
{
    public class UploadPerformanceLogsFunctionParameters
    {
        public string SearchRequestId { get; set; }
        public RequestPerformanceMetrics PerformanceMetrics { get; set; }
    }
}
