using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels
{
    internal class DonorFileWithoutUpdate
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public IEnumerable<DonorUpdate> donors { get; set; }

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
}