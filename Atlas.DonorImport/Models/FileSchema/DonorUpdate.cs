using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    internal class DonorUpdate
    {
        public string RecordId { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public ImportDonorChangeType ChangeType { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public ImportDonorType DonorType { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public string RegistryCode { get; set; }
        
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }
        
        public ImportedHla Hla {get;set;}
        
        public UpdateMode UpdateMode { get; set; }
    }
}