using System;
using System.IO;
using Atlas.Common.Utils.Extensions;

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class DonorImportFile : IDisposable
    {
        public string FileLocation { get; set; }
        public Stream Contents { get; set; }

        public DateTime UploadTime { get; set; }

        internal DateTime TruncatedUploadTime => UploadTime.TruncateToWholeMilliseconds();
        
        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            Contents?.Dispose();
        }

        #endregion
    }
}