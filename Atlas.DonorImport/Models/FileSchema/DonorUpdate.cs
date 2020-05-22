using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    internal class DonorUpdate
    {
        public int RecordId { get; set; }
        public ImportDonorChangeType ImportDonorChangeType { get; set; }
        public ImportDonorType ImportDonorType { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public int RegistryCode { get; set; }
        
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }
        
        public Hla Hla {get;set;}
    }
}