using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Atlas.ManualTesting.Common.Models.Entities;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    public class ValidationSearchRequestRecord : SearchRequestRecord
    {
        public int SearchSet_Id { get; set; }
    }

    internal static class SearchRequestRecordBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<ValidationSearchRequestRecord> modelBuilder)
        {
            modelBuilder
                .HasOne<SearchSet>()
                .WithMany()
                .HasForeignKey(r => r.SearchSet_Id);

            modelBuilder
                .HasOne<SubjectInfo>()
                .WithMany()
                .HasForeignKey(r => r.PatientId);

            modelBuilder.HasIndex(r => r.AtlasSearchIdentifier);
        }
    }
}