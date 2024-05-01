using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchPrediction.Test.Validation.Data.Models.Homework
{
    public class ImputationSummary
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string ExternalSubjectId { get; set; }

        public int HfSetPopulationId { get; set; }
        public bool WasRepresented { get; set; }
        public int GenotypeCount { get; set; }

        [Column(TypeName = "decimal(21,20)")]
        public decimal SumOfLikelihoods { get; set; }
    }

    internal static class ImputationSummaryBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<ImputationSummary> modelBuilder)
        {
            modelBuilder.HasIndex(x => new { x.ExternalSubjectId });
        }
    }
}