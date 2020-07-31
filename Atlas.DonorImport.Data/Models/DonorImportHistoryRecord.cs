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
        public DonorImportState State { get; set; }

        public string FileState
        {
            get => State.ToString();
            // ReSharper disable once ValueParameterNotUsed - used by EF
            set => State = Enum.Parse<DonorImportState>(FileState);
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