namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Request object for the donor import debug endpoint.
    /// </summary>
    public class DonorImportRequest
    {
        /// <summary>
        /// Name to be assigned to the file upload.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Contents of the donor import file.
        /// </summary>
        public DonorImportFileContents FileContents { get; set; }
    }
}
