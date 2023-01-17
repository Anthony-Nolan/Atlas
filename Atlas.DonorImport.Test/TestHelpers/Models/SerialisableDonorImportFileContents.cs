using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models
{
    /// <summary>
    /// Override of non-test file schema used to add attributes only needed for serialisation in tests
    /// </summary>
    internal class SerialisableDonorImportFileContents : DonorImportFileSchema
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public override UpdateMode updateMode { get; set; }

        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 2)]
        public override IEnumerable<DonorUpdate> donors { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
}