using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace Atlas.DonorImport.Models.FileSchema
{
    public class Hla
    {
        public Locus A { get; set; }
        public Locus B { get; set; }
        public Locus C { get; set; }
        public Locus DPB1 { get; set; }
        public Locus DQB1 { get; set; }
        public Locus DRB1 { get; set; }
    }

    public class Locus
    {
        public DnaLocus Dna { get; set; }

        [JsonProperty(PropertyName = "ser")]
        public SerologyLocus Serology { get; set; }
    }

    public class DnaLocus
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }

    public class SerologyLocus
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }
}