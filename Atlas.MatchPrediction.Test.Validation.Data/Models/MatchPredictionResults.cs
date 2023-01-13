using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    public class MatchPredictionResults : IBulkInsertModel
    {
        public int Id { get; set; }
        public int MatchPredictionRequestId { get; set; }

        /// <summary>
        /// This will be null for "cross-loci" predictions, e.g., P(x/10), etc.
        /// </summary>
        [Column(TypeName = "nvarchar(10)")]
        public Locus? Locus { get; set; }

        public int MismatchCount { get; set; }

        /// <summary>
        /// Nullable for when patient and/or donor were non-represented.
        /// </summary>
        [Column(TypeName = "decimal(6,5)")]
        public decimal? Probability { get; set; }
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
