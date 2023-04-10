using System.Text.Json.Serialization;

namespace Atlas.DonorImport.FileSchema.Models.DonorChecker
{
    public class DonorIdCheckerResults
    {
        [JsonPropertyName("donPool")]
        public string RegistryCode { get; set; }
        [JsonPropertyName("donorType")]
        public ImportDonorType DonorType { get; set; }
        [JsonPropertyName("absentRecordIds")]
        public List<string> AbsentRecordIds { get; set; } = new();
        [JsonPropertyName("orphanedRecordIds")]
        public List<string> OrphanedRecordIds { get; set; } = new();
        
    }
}
