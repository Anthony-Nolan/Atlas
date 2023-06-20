using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Data.Models
{
    [Table(TableName)]
    public class HaplotypeFrequency
    {
        internal const string TableName = "HaplotypeFrequencies";
        internal static readonly string QualifiedTableName = $"{MatchPredictionContext.Schema}.{TableName}";

        public long Id { get; set; }

        [Column(TypeName = "decimal(20,20)")]
        public decimal Frequency { get; set; }

        [NotMapped]
        public LociInfo<string> Hla { get; set; } = new();

        [Required]
        [MaxLength(64)]
        public string A
        {
            get => Hla.A;
            set => Hla = Hla.SetLocus(Locus.A, value);
        }

        [Required]
        [MaxLength(64)]
        public string B
        {
            get => Hla.B;
            set => Hla = Hla.SetLocus(Locus.B, value);
        }

        [Required]
        [MaxLength(64)]
        public string C
        {
            get => Hla.C;
            set => Hla = Hla.SetLocus(Locus.C, value);
        }

        [Required]
        [MaxLength(64)]
        public string DQB1
        {
            get => Hla.Dqb1;
            set => Hla = Hla.SetLocus(Locus.Dqb1, value);
        }

        [Required]
        [MaxLength(64)]
        public string DRB1
        {
            get => Hla.Drb1;
            set => Hla = Hla.SetLocus(Locus.Drb1, value);
        }

        public const string SetIdColumnName = "Set_Id";

        [ForeignKey(nameof(SetId))]
        public HaplotypeFrequencySet Set { get; set; }

        /// <summary>
        /// Foreign Key. Should use <see cref="Set"/> navigation property in code instead.
        /// This exists to allow index creation on the generated foreign key. 
        /// </summary>
        [ForeignKey(nameof(Set))]
        [Column(SetIdColumnName)]
        public int SetId { get; set; }

        public HaplotypeTypingCategory TypingCategory { get; set; }
    }

    internal static class HaplotypeFrequencyModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<HaplotypeFrequency> modelBuilder)
        {
            // Index used for ensuring correct data - no duplicate haplotypes allowed
            modelBuilder
                .HasIndex(f => new {f.A, f.B, f.C, f.DQB1, f.DRB1, f.SetId})
                .IsUnique()
                .IncludeProperties(f => new
                {
                    f.Frequency,
                    f.TypingCategory
                });
            
            // Index used for loading a set by id. Ensure all required data is included in this index to avoid clustered index scan.
            modelBuilder
                .HasIndex(f => new {f.SetId})
                .IncludeProperties(f => new
                {
                    f.Id,
                    f.A,
                    f.B,
                    f.C,
                    f.DQB1,
                    f.DRB1,
                    f.Frequency,
                    f.TypingCategory
                });
        }
    }
}