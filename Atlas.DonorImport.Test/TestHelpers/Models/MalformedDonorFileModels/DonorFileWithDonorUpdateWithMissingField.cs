using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlas.DonorImport.FileSchema.Models;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels
{
    internal class DonorFileWithDonorUpdateWithMissingField
    {
        // ReSharper disable once InconsistentNaming
        [JsonProperty(Order = 1)]
        public UpdateMode updateMode { get; set; }
        
        [JsonProperty(Order = 2)]
        public IEnumerable<DonorUpdateWithMissingField> donors { get; set; }
        
        public Stream ToStream()
        {
            var fileJson = JsonConvert.SerializeObject(this);
            return new MemoryStream(Encoding.Default.GetBytes(fileJson));
        }
    }
    
    internal class DonorUpdateWithMissingField
    {
        public DonorUpdateWithMissingField(DonorUpdate donor)
        {
            // record ID explicitly excluded
            ChangeType = donor.ChangeType;
            DonorType = donor.DonorType;
            RegistryCode = donor.RegistryCode;
            Hla = donor.Hla;
            UpdateMode = donor.UpdateMode;
            Ethnicity = donor.Ethnicity;
        }
        
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }
        public ImportDonorChangeType ChangeType { get; set; }
        public ImportDonorType DonorType { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public string RegistryCode { get; set; }
        
        public ImportedHla Hla {get;set;}
        
        public UpdateMode UpdateMode { get; set; }
    }
}