using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    public enum ChangeType
    {
        [EnumMember(Value = "N")] Create,
        [EnumMember(Value = "D")] Delete,
        [EnumMember(Value = "U")] Update,
    }
}