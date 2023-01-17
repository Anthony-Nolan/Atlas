using Newtonsoft.Json;

namespace Atlas.DonorImport.FileSchema.Models
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global - Instantiated by JSON parser
    public class ImportedHla
    {
        public ImportedLocus A { get; set; }
        public ImportedLocus B { get; set; }
        public ImportedLocus C { get; set; }
        public ImportedLocus DPB1 { get; set; }
        public ImportedLocus DQB1 { get; set; }
        public ImportedLocus DRB1 { get; set; }
    }

    public class ImportedLocus
    {
        public TwoFieldStringData Dna { get; set; }

        [JsonProperty(PropertyName = "ser")]
        public TwoFieldStringData Serology { get; set; }
    }

    public class TwoFieldStringData
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }
}