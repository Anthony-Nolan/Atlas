using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    // ReSharper disable InconsistentNaming

    public class MatchedDonor : IModel
    {
        public int Id { get; set; }
        public int SearchRequestRecord_Id { get; set; }
        public int MatchedDonorSimulant_Id { get; set; }
        public int TotalMatchCount { get; set; }
        public int TypedLociCount { get; set; }
        public bool? WasPatientRepresented { get; set; }
        public bool? WasDonorRepresented { get; set; }

        /// <summary>
        /// Serialised copy of the <see cref="Client.Models.Search.Results.Matching.MatchingAlgorithmResult"/>.
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string MatchingResult { get; set; }

        /// <summary>
        /// Serialised copy of the <see cref="Client.Models.Search.Results.MatchPrediction.MatchProbabilityResponse"/>.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string MatchPredictionResult { get; set; }
    }

    internal static class MatchedDonorBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MatchedDonor> modelBuilder)
        {
            modelBuilder
                .HasOne<SearchRequestRecord>()
                .WithMany()
                .HasForeignKey(r => r.SearchRequestRecord_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne<Simulant>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonorSimulant_Id);

            modelBuilder
                .HasIndex(r => new { r.SearchRequestRecord_Id, r.MatchedDonorSimulant_Id, r.TotalMatchCount });
        }
    }
}
