using System.Text.Json.Serialization;

namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    /// <summary>
    /// Results for the donor ID checker
    /// </summary>
    public class DonorIdCheckerResults
    {
        /// <summary>
        /// List of donor ids that are not present in a system
        /// </summary>
        [JsonPropertyName("missingRecordIds")]
        public List<string> MissingRecordIds { get; set; } = new();
    }
}