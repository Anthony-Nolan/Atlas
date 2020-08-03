namespace Atlas.DonorImport.Data.Models
{
    public enum DonorImportState
    {
        NotFound = 0,
        Started = 1,
        Completed = 2,
        FailedPermanent = 3,
        FailedUnexpectedly = 4
    }
}