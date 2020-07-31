using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.DonorImport.Data.Models
{
    [Table("DonorImportHistory")]
    public class DonorImportRecordHistory
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
            set => State = Enum.Parse<DonorImportState>(FileState);
        }
        
        public DateTime LastUpdated { get; set; }
    }

    internal static class DonorImportHistoryModelBuilder
    {
        public static void SetUpDonorImportHistory(this EntityTypeBuilder<DonorImportRecordHistory> donorHistoryModel)
        {
            donorHistoryModel.HasKey(d => new {d.Filename, d.UploadTime});
            donorHistoryModel.Property(d => d.Id).ValueGeneratedOnAdd();
            donorHistoryModel.Property(d => d.LastUpdated).ValueGeneratedOnAddOrUpdate();
        }
    }
}