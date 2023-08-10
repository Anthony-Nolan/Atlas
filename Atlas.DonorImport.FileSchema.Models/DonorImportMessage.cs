namespace Atlas.DonorImport.FileSchema.Models
{
    public class DonorImportMessage
    {
        public string FileName { get; set; }
        public bool WasSuccessful { get; set; }
        public int ImportedDonorCount { get; set; }
        public int FailedDonorCount { get; set; }
    }
}
