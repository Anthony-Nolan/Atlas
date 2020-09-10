using System;
using System.IO;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class DonorImportFile : IDisposable
    {
        public string FileLocation { get; set; }
        public Stream Contents { get; set; }
        public DateTime UploadTime { get; set; }
        public string MessageId { get; set; }

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            Contents?.Dispose();
        }

        #endregion
    }
}