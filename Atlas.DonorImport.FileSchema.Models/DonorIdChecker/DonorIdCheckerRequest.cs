namespace Atlas.DonorImport.FileSchema.Models.DonorIdChecker
{
    /// <summary>
    /// Request to check whether a set of donors exist in the donor store or not, by their record IDs.
    /// </summary>
    public class DonorIdCheckerRequest
    {
        /// <summary>
        /// List of alphanumeric donor record Ids.
        /// </summary>
        public IEnumerable<string> recordIds { get; set; }
    }
}