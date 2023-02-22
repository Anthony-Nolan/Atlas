namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    /// <summary>
    /// Results for the donor ID checker
    /// </summary>
    public class DonorIdCheckerResults
    {
        public List<DonorIdCheckerResult> Results { get; set; } = new();
    }

    public class DonorIdCheckerResult
    {
        /// <summary>
        /// Alphanumeric donor Id
        /// </summary>
        public string RecordId { get; set; }

        /// <summary>
        /// Is the donor present in the donor store or not
        /// </summary>
        public bool IsPresentInDonorStore { get; set; }
    }
}