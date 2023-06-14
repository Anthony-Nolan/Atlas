namespace Atlas.ManualTesting.Models
{
    public class SearchOutcomesProcessingError
    {
        public string BlobContainer { get; set; }
        public string LogFileName { get; set; }
        public string ExceptionMessage { get; set; }
    }
}
