using System;
using System.IO;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class BlobImportFile : IDisposable
    {
        public string FileLocation { get; set; }
        public Stream Contents { get; set; }
        
        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            Contents?.Dispose();
        }

        #endregion
    }
}
