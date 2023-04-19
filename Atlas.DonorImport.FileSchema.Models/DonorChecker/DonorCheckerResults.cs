namespace Atlas.DonorImport.FileSchema.Models.DonorChecker
{
    /// <summary>
    /// Results for the donor checker
    /// </summary>
    public class DonorCheckerResults : IDonorCheckerResults
    {
        /// <summary>
        /// List of donor ids that returned by donor checker
        /// </summary>
        public List<string> DonorRecordIds { get; set; } = new();
    }
}