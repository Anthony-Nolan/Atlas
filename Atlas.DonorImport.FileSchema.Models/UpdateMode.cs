using System.Runtime.Serialization;

namespace Atlas.DonorImport.FileSchema.Models
{
    /// <summary>
    /// Used to differentiate between the type of file being uploaded.
    /// All files will use the same differential schema - this is used to distinguish an initial load of donors from ongoing additions of new donors.
    /// </summary>
    public enum UpdateMode
    {
        [EnumMember(Value = "diff")] Differential,
        [EnumMember(Value = "full")] Full,
        [EnumMember(Value = "cmpr")] Compare
    }
}