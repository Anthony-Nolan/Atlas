using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    public enum DonorType
    {
        [EnumMember(Value = "D")] Adult,
        [EnumMember(Value = "C")] Cord
    }
}