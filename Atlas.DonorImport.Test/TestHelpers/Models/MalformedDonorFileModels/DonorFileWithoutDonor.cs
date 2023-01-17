using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels
{
    internal class DonorFileWithoutDonor
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public UpdateMode updateMode { get; set; }
        

        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
}