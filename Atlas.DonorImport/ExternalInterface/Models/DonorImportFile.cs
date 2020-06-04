using System;
using System.IO;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class DonorImportFile : IDisposable
    {
        public string FileName { get; set; }
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