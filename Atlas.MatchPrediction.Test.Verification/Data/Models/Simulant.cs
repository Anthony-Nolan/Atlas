using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    // ReSharper disable InconsistentNaming

    public class Simulant
    {
        public int Id { get; set; }

        public int TestHarness_Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public TestIndividualCategory TestIndividualCategory { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public SimulatedHlaTypingCategory SimulatedHlaTypingCategory { get; set; }

        [Required]
        [MaxLength(64)]
        public string A_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string A_2 { get; set; }

        [Required]
        [MaxLength(64)]
        public string B_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string B_2 { get; set; }

        [Required]
        [MaxLength(64)]
        public string C_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string C_2 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DQB1_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DQB1_2 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1_2 { get; set; }

        /// <summary>
        /// Only relevant to Masked simulants: Id of simulant whose genotype was masked to generate this phenotype.
        /// </summary>
        public int? SourceSimulantId { get; set; }

        public static IReadOnlyCollection<string> GetColumnNamesForBulkInsert()
        {
            var columns = typeof(Simulant).GetProperties().Select(p => p.Name).ToList();
            columns.Remove(nameof(Id));
            return columns;
        }
    }

    internal static class SimulantModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<Simulant> modelBuilder)
        {
            modelBuilder
                .HasIndex(s => new { s.TestHarness_Id, s.TestIndividualCategory, s.SimulatedHlaTypingCategory });

            modelBuilder
                .HasOne<TestHarness>()
                .WithMany()
                .HasForeignKey(s => s.TestHarness_Id);

            modelBuilder
                .Property(e => e.TestIndividualCategory)
                .HasConversion<string>();

            modelBuilder
                .Property(e => e.SimulatedHlaTypingCategory)
                .HasConversion<string>();
        }
    }
}
