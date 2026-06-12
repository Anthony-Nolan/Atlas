using System.Text.Json.Serialization;

namespace Atlas.DonorImport.FileSchema.Models;

// Note that for optimal efficiency, donor update files are streamed rather than deserialised directly. 
// This file exists to give an at-a-glance view of the full expected file schema
public abstract class DonorImportFileSchema 
{
    [JsonPropertyOrder(0)]
    public abstract UpdateMode updateMode { get; set; }
    public abstract IEnumerable<DonorUpdate> donors { get; set; }
}