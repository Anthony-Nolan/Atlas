using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    internal static class MatchedDonorBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonor> modelBuilder)
        {
            modelBuilder
                .HasOne<ValidationSearchRequestRecord>()
                .WithMany()
                .HasForeignKey(r => r.SearchRequestRecord_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne<SubjectInfo>()
                .WithMany()
                .HasForeignKey(r => r.DonorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasIndex(r => new { r.SearchRequestRecord_Id, r.DonorId, r.TotalMatchCount });
        }
    }
}