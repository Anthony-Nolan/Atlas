using System;
using System.Runtime.Serialization;
using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Models.FileSchema
{
    /// <summary>
    /// Donor Type as defined by the schema agreed for external use when uploading donor imports.  
    /// </summary>
    internal enum ImportDonorType
    {
        [EnumMember(Value = "D")] Adult,
        [EnumMember(Value = "C")] Cord,
        /// <summary>
        /// Banked donors are in the agreed schema for donor uploads, but are not yet supported by the Atlas system.
        /// </summary>
        [EnumMember(Value = "B")] Banked
    }
    
    internal static class DonorTypeExtensions
    {
        public static DatabaseDonorType ToDatabaseType(this ImportDonorType fileDonorType)
        {
            return fileDonorType switch
            {
                ImportDonorType.Adult => DatabaseDonorType.Adult,
                ImportDonorType.Cord => DatabaseDonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(fileDonorType), fileDonorType, null)
            };
        }
    }
}