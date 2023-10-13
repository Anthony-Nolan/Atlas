using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.DonorImport.FileSchema.Models
{
    public abstract class DonorImportMessage
    {
        public string FileName { get; set; }
        public abstract bool WasSuccessful { get; }
    }

    public class SuccessDonorImportMessage : DonorImportMessage
    {
        public override bool WasSuccessful => true;
        public int ImportedDonorCount { get; set; }
        public int FailedDonorCount { get; set; }
        public IReadOnlyCollection<FauilureSummary> FailedDonorSummary { get; set; }
    }

    public class FailedDonorImportMessage : DonorImportMessage
    {
        public override bool WasSuccessful => false;
        public ImportFailureReason FailureReason { get; set; }
        public string FailureReasonDescription { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ImportFailureReason
    {
        ErrorDuringImport,
        RequestDeadlettered
    }

    public class FauilureSummary
    {
        public string Reason { get; set; }
        public int Count { get; set; }
    }
}
