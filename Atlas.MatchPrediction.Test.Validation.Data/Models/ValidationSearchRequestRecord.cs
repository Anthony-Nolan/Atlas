using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

            modelBuilder
                .Property(r => r.SearchResultsRetrieved)
                .HasDefaultValue(false);

            modelBuilder.HasIndex(r => r.AtlasSearchIdentifier);
            modelBuilder.HasIndex(r => r.WasSuccessful);
        }
    }
}