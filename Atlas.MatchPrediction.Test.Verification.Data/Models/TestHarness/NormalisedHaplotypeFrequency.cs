using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness
{
    public class NormalisedHaplotypeFrequency : IModel
    {
        public int Id { get; set; }

        public int NormalisedPool_Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string A { get; set; }

        [Required]
        [MaxLength(64)]
        public string B { get; set; }

        [Required]
        [MaxLength(64)]
        public string C { get; set; }

        [Required]
        [MaxLength(64)]
        public string DQB1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1 { get; set; }

        [Required]
        [Column(TypeName = "decimal(20,20)")]
        public decimal Frequency { get; set; }

        [Required]
        public int CopyNumber { get; set; }
    }

    internal static class NormalisedHaplotypeFrequencyModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<NormalisedHaplotypeFrequency> modelBuilder)
        {
            modelBuilder
                .HasOne<NormalisedPool>()
                .WithMany()
                .HasForeignKey(n => n.NormalisedPool_Id);
        }
    }
}