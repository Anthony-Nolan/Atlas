using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    public class SearchSet
    {
        public int Id { get; set; }
        public int TestDonorExportRecord_Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string DonorType { get; set; }

        public int MismatchCount { get; set; }

        [Column(TypeName = "nvarchar(256)")]
        public string MatchLoci { get; set; }

        public bool SearchRequestsSubmitted { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
    }

    internal static class SearchSetBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SearchSet> modelBuilder)
        {
            modelBuilder
                .Property(t => t.CreatedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .Property(t => t.SearchRequestsSubmitted)
                .HasDefaultValue(false);

            modelBuilder
                .HasOne<TestDonorExportRecord>()
                .WithMany()
                .HasForeignKey(t => t.TestDonorExportRecord_Id);
        }
    }
}
