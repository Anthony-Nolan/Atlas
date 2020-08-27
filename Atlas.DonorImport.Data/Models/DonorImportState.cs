namespace Atlas.DonorImport.Data.Models
{
    public enum DonorImportState
    {
        Started = 1,
        Completed = 2,
        FailedPermanent = 3,
        FailedUnexpectedly = 4,
        Stalled = 5
    }
}