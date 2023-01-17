using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels
{
    internal class DonorFileWithDonorUpdateInvalidEnum
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public string updateMode { get; set; }
        
        [JsonProperty(Order = 2)]
        public IEnumerable<DonorUpdateWithInvalidEnums> donors { get; set; }
        
        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
    internal class DonorUpdateWithInvalidEnums
    {
        public string RecordId { get; set; }
        public string ChangeType { get; set; }
        public string DonorType { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public string RegistryCode { get; set; }
        
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }
        
        public ImportedHla Hla {get;set;}
        
        public string UpdateMode { get; set; }
    }
    
}