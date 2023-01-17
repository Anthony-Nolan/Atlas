using System.Runtime.Serialization;

namespace Atlas.DonorImport.FileSchema.Models
{
    /// <summary>
    /// Per-donor operation type.
    /// </summary>
    ///
    public enum ImportDonorChangeType
    {
        [EnumMember(Value = "N")] Create,
        [EnumMember(Value = "D")] Delete,
        [EnumMember(Value = "U")] Edit,
        [EnumMember(Value = "NU")] Upsert, // NewOrUpdate
    }

}