using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.DonorImport.Data.Context;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.DonorImport.Data.Models
{
    [Table(TableName)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DonorImportHistoryRecord
    {
        internal const string TableName = "DonorImportHistory";
        internal static readonly string QualifiedTableName = $"{DonorContext.Schema}.{TableName}";
        
        public int Id { get; set; }

        [Required]
        public string ServiceBusMessageId { get; set; }

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

        /// <summary>
        /// The time that this upload started processing.
        /// When retrying due to transient failures, this will be the most recent invocation, to give accurate import timing information.
        /// </summary>
        public DateTime ImportBegin { get; set; }

        /// <summary>
        /// The time that this upload successfully finished processing.
        /// </summary>
        public DateTime? ImportEnd { get; set; }

        /// <summary>
        /// The number of times this upload has failed.
        /// Counts both permanent and transient errors. 
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// The number of donors that have already been been imported.
        /// When retrying due to transient failures, it shows what updates have already been applied and at what point in the file to start retrying from.
        /// </summary>
        public int ImportedDonorsCount { get; set; }
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