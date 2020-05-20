using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    public class DonorUpdate
    {
        public int RecordId { get; set; }
        public ChangeType ChangeType { get; set; }
        public DonorType DonorType { get; set; }

        [JsonProperty(PropertyName = "donPool")]
        public int RegistryCode { get; set; }
        
        // ReSharper disable once StringLiteralTypo
        [JsonProperty(PropertyName = "ethn")]
        public string Ethnicity { get; set; }
        
        public Hla Hla {get;set;}
    }
}