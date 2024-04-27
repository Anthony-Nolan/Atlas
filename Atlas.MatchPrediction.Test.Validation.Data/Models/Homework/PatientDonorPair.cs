using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models.Homework
{
    public class PatientDonorPair : IBulkInsertModel
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string PatientId { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string DonorId { get; set; }

        /// <summary>
        /// Has this <see cref="PatientDonorPair"/> been fully processed or are steps remaining?
        /// </summary>
        [BulkInsertIgnoreAttribute]
        public bool IsProcessed { get; set; }

        [BulkInsertIgnoreAttribute]
        public bool? DidPatientHaveMissingHla { get; set; }

        [BulkInsertIgnoreAttribute]
        public bool? DidDonorHaveMissingHla { get; set; }

        [BulkInsertIgnoreAttribute]
        public bool? PatientImputationCompleted { get; set; }

        [BulkInsertIgnoreAttribute]
        public bool? DonorImputationCompleted { get; set; }

        [BulkInsertIgnoreAttribute]
        public bool? MatchingGenotypesCalculated { get; set; }

        public int HomeworkSet_Id { get; set; }
    }

    internal static class PatientDonorPairBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<PatientDonorPair> modelBuilder)
        {
            modelBuilder
                .Property(t => t.IsProcessed)
                .HasDefaultValue(false);

            modelBuilder
                .HasOne<HomeworkSet>()
                .WithMany()
                .HasForeignKey(x => x.HomeworkSet_Id);

            modelBuilder
                .HasIndex(x => new { x.DonorId, x.PatientId, x.HomeworkSet_Id, IsProcessingComplete = x.IsProcessed })
                .IsUnique();
        }
    }
}
