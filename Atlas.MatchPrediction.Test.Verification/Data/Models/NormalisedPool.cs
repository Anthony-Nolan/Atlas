using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class NormalisedPool : ParentEntityBase
    {
    }

    internal static class NormalisedPoolBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<NormalisedPool> modelBuilder)
        {
            modelBuilder
                .Property(t => t.CreatedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
