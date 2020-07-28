using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequency
    {
        public long Id { get; set; }

        [Column(TypeName = "decimal(20,20)")]
        public decimal Frequency { get; set; }

        [NotMapped]
        public LociInfo<string> Hla { get; set; } = new LociInfo<string>();

        [Required]
        [MaxLength(64)]
        public string A
        {
            get => Hla.A;
            set => Hla.SetLocus(Locus.A, value);
        }

        [Required]
        [MaxLength(64)]
        public string B
        {
            get => Hla.B;
            set => Hla.SetLocus(Locus.B, value);
        }

        [Required]
        [MaxLength(64)]
        public string C
        {
            get => Hla.C;
            set => Hla.SetLocus(Locus.C, value);
        }

        [Required]
        [MaxLength(64)]
        public string DQB1
        {
            get => Hla.Dqb1;
            set => Hla.SetLocus(Locus.Dqb1, value);
        }

        [Required]
        [MaxLength(64)]
        public string DRB1
        {
            get => Hla.Drb1;
            set => Hla.SetLocus(Locus.Drb1, value);
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
            modelBuilder
                .HasIndex(f => new {f.A, f.B, f.C, f.DQB1, f.DRB1, f.SetId})
                .IsUnique()
                .IncludeProperties(f => f.Frequency);
        }
    }

    internal static class HaplotypeFrequencyExtensions
    {
        public static LociInfo<string> Haplotype(this HaplotypeFrequency haplotypeFrequency)
        {
            return new LociInfo<string>
            {
                A = haplotypeFrequency.A,
                B = haplotypeFrequency.B,
                C = haplotypeFrequency.C,
                Dpb1 = default,
                Dqb1 = haplotypeFrequency.DQB1,
                Drb1 = haplotypeFrequency.DRB1,
            };
        }
    }
}