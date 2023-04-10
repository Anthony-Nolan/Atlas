// ReSharper disable InconsistentNaming - As we are not deserializing this file directly, we must keep the property names in sync with the expected schema, including casing
namespace Atlas.DonorImport.FileSchema.Models.DonorChecker
{
    /// <summary>
    /// Request to check whether a set of donors exist in the donor store or not, by their record IDs.
    /// </summary>
    public class DonorIdCheckerRequest
    {
        public string donPool { get; set; }
        public string donorType { get; set; }
        /// <summary>
        /// List of alphanumeric donor record Ids.
        /// </summary>
        public IEnumerable<string> donors { get; set; }
    }
}