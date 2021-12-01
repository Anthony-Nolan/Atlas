using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    /// <summary>
    /// Per-donor operation type.
    /// </summary>
    ///
    internal enum ImportDonorChangeType
    {
        [EnumMember(Value = "N")] Create,
        [EnumMember(Value = "D")] Delete,
        [EnumMember(Value = "U")] Edit,
        [EnumMember(Value = "NU")] Upsert, // NewOrUpdate
    }

}