using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global - instantiated via JSON deserialization 

namespace Atlas.DonorImport.Models.FileSchema
{
    internal class Hla
    {
        public Locus A { get; set; }
        public Locus B { get; set; }
        public Locus C { get; set; }
        public Locus DPB1 { get; set; }
        public Locus DQB1 { get; set; }
        public Locus DRB1 { get; set; }
    }

    internal class Locus
    {
        public DnaLocus Dna { get; set; }

        [JsonProperty(PropertyName = "ser")]
        public SerologyLocus Serology { get; set; }
    }

    internal class DnaLocus
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }

    internal class SerologyLocus
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }
}