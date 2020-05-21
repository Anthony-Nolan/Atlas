using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global - Instantiated by JSON parser
    // ReSharper disable UnusedAutoPropertyAccessor.Global
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
        // ReSharper disable once MemberCanBePrivate.Global - Needed for JSON parsing
        public DnaLocus Dna { get; set; }

        [JsonProperty(PropertyName = "ser")]
        public SerologyLocus Serology { get; set; }

        public string Field1 => Dna?.Field1 ?? Serology?.Field1;
        public string Field2 => Dna?.Field2 ?? Serology?.Field2;
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