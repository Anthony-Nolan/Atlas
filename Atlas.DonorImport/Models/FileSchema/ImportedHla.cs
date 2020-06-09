using System;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Models.FileSchema
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global - Instantiated by JSON parser
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    internal class ImportedHla
    {
        public ImportedLocus A { get; set; }
        public ImportedLocus B { get; set; }
        public ImportedLocus C { get; set; }
        public ImportedLocus DPB1 { get; set; }
        public ImportedLocus DQB1 { get; set; }
        public ImportedLocus DRB1 { get; set; }
    }

    internal class ImportedLocus
    {
        // ReSharper disable once MemberCanBePrivate.Global - Needed for JSON parsing
        [Obsolete("Access via ReadField1 and ReadField2, not directly - this property is only for deserialization purposes.")]
        public TwoFieldStringData Dna { get; set; }

        [Obsolete("Access via ReadField1 and ReadField2, not directly - this property is only for deserialization purposes.")]
        [JsonProperty(PropertyName = "ser")]
        public TwoFieldStringData Serology { get; set; }
    }

    internal class TwoFieldStringData
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
    }
}