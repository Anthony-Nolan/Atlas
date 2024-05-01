using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models.Homework
{
    public class HomeworkSet
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(256)")]
        public string SetName { get; set; }

        [Column(TypeName = "nvarchar(128)")]
        public string MatchLoci { get; set; }

        [Column(TypeName = "nvarchar(8)")]
        public string HlaNomenclatureVersion { get; set; }

        public DateTimeOffset SubmittedDateTime { get; set; }
    }

    internal static class HomeworkSetBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<HomeworkSet> modelBuilder)
        {
            modelBuilder
                .Property(t => t.SubmittedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.HasIndex(t => t.SetName);
        }
    }
}