using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class AzureStorageSettings
    {
        [Required(AllowEmptyStrings = false)]
        public string ConnectionString { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string MatchPredictionResultsBlobContainer { get; set; }
    }
}