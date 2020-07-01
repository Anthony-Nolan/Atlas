using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    }

    internal static class HaplotypeFrequencyModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<HaplotypeFrequency> modelBuilder)
        {
            modelBuilder
                .HasIndex(f => new {f.A, f.B, f.C, f.DQB1, f.DRB1, f.SetId})
                .IncludeProperties(f => f.Frequency);
        }
    }
}