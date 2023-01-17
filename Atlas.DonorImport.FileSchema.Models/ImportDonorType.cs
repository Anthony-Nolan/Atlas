using System.Runtime.Serialization;

namespace Atlas.DonorImport.FileSchema.Models
{
    /// <summary>
    /// Donor Type as defined by the schema agreed for external use when uploading donor imports.  
    /// </summary>
    public enum ImportDonorType
    {
        [EnumMember(Value = "D")] Adult,
        [EnumMember(Value = "C")] Cord,
        /// <summary>
        /// Banked donors are in the agreed schema for donor uploads, but are not yet supported by the Atlas system.
        /// </summary>
        [EnumMember(Value = "B")] Banked
    }
}