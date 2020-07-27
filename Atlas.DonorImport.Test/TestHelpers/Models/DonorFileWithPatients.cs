using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models
{
    /// <summary>
    /// Donor import file schema which has a patients column instead of donors.
    /// </summary>
    internal class DonorFileWithPatients
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public UpdateMode updateMode { get; set; }

        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 2)]
        public IEnumerable<DonorUpdate> patients { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
}