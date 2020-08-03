using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.DonorImport.Data.Models
{
    [Table("DonorImportHistory")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DonorImportHistoryRecord
    {
        public int Id { get; set; }
        /// <summary>
        ///  The filename of a donor import file
        /// </summary>
        [Required]
        public string Filename { get; set; }
        
        /// <summary>
        /// The time a file was uploaded to blob storage
        /// </summary>
        [Required]
        public DateTime UploadTime { get; set; }
        
        [NotMapped]
        public DonorImportState FileState { get; set; }

        [Column("FileState")]
        [Obsolete("Only for use by EF. Use FileState enum property directly.")]
        // ReSharper disable once UnusedMember.Global - used by EF
        public string FileStateString
        {
            get => FileState.ToString();
            // ReSharper disable once ValueParameterNotUsed - used by EF
            set => FileState = Enum.Parse<DonorImportState>(FileStateString);
        }
        
        public DateTime LastUpdated { get; set; }
    }

    internal static class DonorImportHistoryModelBuilder
    {
        public static void SetUpDonorImportHistory(this EntityTypeBuilder<DonorImportHistoryRecord> donorHistoryModel)
        {
            donorHistoryModel.HasKey(d => new {d.Filename, d.UploadTime});
            donorHistoryModel.Property(d => d.Id).ValueGeneratedOnAdd();
            donorHistoryModel.Property(d => d.LastUpdated).ValueGeneratedOnAddOrUpdate();
        }
    }
}