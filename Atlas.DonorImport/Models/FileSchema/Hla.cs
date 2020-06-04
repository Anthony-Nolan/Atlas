using System;
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
        [Obsolete("Access via Field1 and Field2, not directly - this property is only for deserialization purposes.")]
        public DnaLocus Dna { get; set; }

        [Obsolete("Access via Field1 and Field2, not directly - this property is only for deserialization purposes.")]
        [JsonProperty(PropertyName = "ser")]
        public SerologyLocus Serology { get; set; }

#pragma warning disable 618 // Dna & Serology are not Obsolete, but would be considered private if not for deserialization to this class
        [JsonIgnore]
        public string Field1 => Dna?.Field1 ?? Serology?.Field1;
        [JsonIgnore] 
        public string Field2 => Dna?.Field2 ?? Serology?.Field2;
    }

    internal class DnaLocus
    {
        [JsonIgnore]
        private string raw1;
        public string Field1 { get => raw1; set => raw1 = string.IsNullOrEmpty(value) ? null : value; }
        [JsonIgnore]
        private string raw2;
        public string Field2 { get => raw2; set => raw2 = string.IsNullOrEmpty(value) ? null : value; }
    }

    internal class SerologyLocus
    {
        [JsonIgnore]
        private string raw1;
        public string Field1 { get => raw1; set => raw1 = string.IsNullOrEmpty(value) ? null : value; }
        [JsonIgnore]
        private string raw2;
        public string Field2 { get => raw2; set => raw2 = string.IsNullOrEmpty(value) ? null : value; }
    }
}