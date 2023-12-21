using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    public class MatchPredictionResults : MatchProbability
    {
        public int MatchPredictionRequestId { get; set; }
    }

    internal static class MatchPredictionResultsBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchPredictionResults> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchPredictionRequest>()
                .WithMany()
                .HasForeignKey(t => t.MatchPredictionRequestId);

            modelBuilder
                .HasIndex(x => new { x.MatchPredictionRequestId, x.Locus, x.MismatchCount });
        }
    }
}
