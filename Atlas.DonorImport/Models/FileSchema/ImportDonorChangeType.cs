using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    internal enum ImportDonorChangeType
    {
        [EnumMember(Value = "N")] Create,
        [EnumMember(Value = "D")] Delete,
        [EnumMember(Value = "U")] Update,
    }
}