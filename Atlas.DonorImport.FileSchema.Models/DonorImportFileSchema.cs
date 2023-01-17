// ReSharper disable InconsistentNaming - As we are not deserialising this file directly, we must keep the property names in sync with the expected schema, including casing

using Newtonsoft.Json;

namespace Atlas.DonorImport.FileSchema.Models
{
    // Note that for optimal efficiency, donor update files are streamed rather than deserialised directly. 
    // This file exists to give an at-a-glance view of the full expected file schema
    public abstract class DonorImportFileSchema 
    {
        [JsonProperty(Order = 1)]
        public abstract UpdateMode updateMode { get; set; }
        public abstract IEnumerable<DonorUpdate> donors { get; set; }
    }
}