using System;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class DonorImportFile : BlobImportFile
    {
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// This is the service bus message id, and thus will be different even if eventgrid sends a notification multiple times for the same upload.
        /// </summary>
        public string MessageId { get; set; }
    }
}