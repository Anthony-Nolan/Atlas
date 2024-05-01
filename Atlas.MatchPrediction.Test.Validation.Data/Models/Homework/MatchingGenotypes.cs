using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Data.Models.Homework
{
    /// <summary>
    /// It's possible C and/or DQB1 will be null for match predictions less than P(x/10)
    /// </summary>
    public class MatchingGenotypes : IBulkInsertModel
    {
        public int Id { get; set; }

        public int TotalCount { get; set; }
        public int A_Count { get; set; }
        public int B_Count { get; set; }
        public int? C_Count { get; set; }
        public int? DQB1_Count { get; set; }
        public int DRB1_Count { get; set; }

        [MaxLength(32)]
        public string Patient_A_1 { get; set; }

        [MaxLength(32)]
        public string Patient_A_2 { get; set; }

        [MaxLength(32)]
        public string Patient_B_1 { get; set; }

        [MaxLength(32)]
        public string Patient_B_2 { get; set; }

        [MaxLength(32)]
        public string? Patient_C_1 { get; set; }

        [MaxLength(32)]
        public string? Patient_C_2 { get; set; }

        [MaxLength(32)]
        public string? Patient_DQB1_1 { get; set; }

        [MaxLength(32)]
        public string? Patient_DQB1_2 { get; set; }

        [MaxLength(32)]
        public string Patient_DRB1_1 { get; set; }

        [MaxLength(32)]
        public string Patient_DRB1_2 { get; set; }

        [Column(TypeName = "decimal(21,20)")]
        public decimal Patient_Likelihood { get; set; }

        [MaxLength(32)]
        public string Donor_A_1 { get; set; }

        [MaxLength(32)]
        public string Donor_A_2 { get; set; }

        [MaxLength(32)]
        public string Donor_B_1 { get; set; }

        [MaxLength(32)]
        public string Donor_B_2 { get; set; }

        [MaxLength(32)]
        public string? Donor_C_1 { get; set; }

        [MaxLength(32)]
        public string? Donor_C_2 { get; set; }

        [MaxLength(32)]
        public string? Donor_DQB1_1 { get; set; }

        [MaxLength(32)]
        public string? Donor_DQB1_2 { get; set; }

        [MaxLength(32)]
        public string Donor_DRB1_1 { get; set; }

        [MaxLength(32)]
        public string Donor_DRB1_2 { get; set; }

        [Column(TypeName = "decimal(21,20)")]
        public decimal Donor_Likelihood { get; set; }

        public int Patient_ImputationSummary_Id { get; set; }
        public int Donor_ImputationSummary_Id { get; set; }
    }

    internal static class MatchingGenotypesBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchingGenotypes> modelBuilder)
        {
            modelBuilder
                .HasOne<ImputationSummary>()
                .WithMany()
                .HasForeignKey(x => x.Patient_ImputationSummary_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne<ImputationSummary>()
                .WithMany()
                .HasForeignKey(x => x.Donor_ImputationSummary_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.HasIndex(x => new { x.TotalCount });
        }
    }
}
