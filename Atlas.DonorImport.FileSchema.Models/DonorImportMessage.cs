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
    }

    public class FailedDonorImportMessage : DonorImportMessage
    {
        public override bool WasSuccessful => false;
        public ImportFaulireReason FailureReason { get; set; }
        public string FailureReasonDescription { get; set; }
    }

    public enum ImportFaulireReason
    {
        ErrorDuringImport,
        RequestDeadlettered
    }
}
