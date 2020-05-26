using System.Runtime.Serialization;

namespace Atlas.DonorImport.Models.FileSchema
{
    /// <summary>
    /// Used to differentiate between the type of file being uploaded.
    /// All files will use the same differential schema - this is used to distinguish an initial load of donors from ongoing additions of new donors.
    /// </summary>
    internal enum UpdateMode
    {
        [EnumMember(Value = "diff")] Differential,
        [EnumMember(Value = "full")] Full
    }
}