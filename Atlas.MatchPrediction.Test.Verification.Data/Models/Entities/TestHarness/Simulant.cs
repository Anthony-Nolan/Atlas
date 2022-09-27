using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness
{
    // ReSharper disable InconsistentNaming

    public class Simulant : IBulkInsertModel
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

        [MaxLength(64)]
        public string C_1 { get; set; }

        [MaxLength(64)]
        public string C_2 { get; set; }

        [MaxLength(64)]
        public string DQB1_1 { get; set; }

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

    public static class SimulantExtensions
    {
        public static PhenotypeInfo<string> ToPhenotypeInfo(this Simulant simulant)
        {
            return new PhenotypeInfo<string>(
                valueA: new LocusInfo<string>(simulant.A_1, simulant.A_2),
                valueB: new LocusInfo<string>(simulant.B_1, simulant.B_2),
                valueC: new LocusInfo<string>(simulant.C_1, simulant.C_2),
                valueDqb1: new LocusInfo<string>(simulant.DQB1_1, simulant.DQB1_2),
                valueDrb1: new LocusInfo<string>(simulant.DRB1_1, simulant.DRB1_2));
        }
    }
}
