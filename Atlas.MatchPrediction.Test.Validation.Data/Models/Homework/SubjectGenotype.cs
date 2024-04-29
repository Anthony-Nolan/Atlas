using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Data.Models.Homework
{
    public class SubjectGenotype : IBulkInsertModel
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string A_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string A_2 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string B_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string B_2 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string? C_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string? C_2 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string? DQB1_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string? DQB1_2 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string DRB1_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public string DRB1_2 { get; set; }

        [Column(TypeName = "decimal(21,20)")]
        public decimal? Likelihood { get; set; }

        public int ImputationSummary_Id { get; set; }
    }

    internal static class SubjectGenotypeBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SubjectGenotype> modelBuilder)
        {
            modelBuilder
                .HasOne<ImputationSummary>()
                .WithMany()
                .HasForeignKey(x => x.ImputationSummary_Id);
        }
    }
}