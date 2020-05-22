using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlas.DonorImport.Models.FileSchema;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    internal static class DonorImportStreamBuilder
    {
        public static Stream BuildFileStream(IEnumerable<DonorUpdate> donorUpdates)
        {
            var file = new DonorFile {donors = donorUpdates, updateMode = UpdateMode.Differential };
            var fileJson = JsonConvert.SerializeObject(file);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }

        public static Stream BuildFileStream(int numberOfDonors)
        {
            var donorUpdates = Enumerable.Range(0, numberOfDonors).Select(i => DonorUpdateBuilder.New.Build());
            return BuildFileStream(donorUpdates);
        }

        private class DonorFile
        {
            // ReSharper disable once InconsistentNaming
            [JsonProperty(Order = 1)]
            public UpdateMode updateMode { get; set; }
            
            // ReSharper disable once InconsistentNaming
            [JsonProperty(Order = 2)]
            public IEnumerable<DonorUpdate> donors;
        }
    }
}