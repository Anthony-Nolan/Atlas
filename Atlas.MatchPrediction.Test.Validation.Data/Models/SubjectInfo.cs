using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Data.Models
{
    public enum SubjectType
    {
        Donor,
        Patient
    }

    public class SubjectInfo : IBulkInsertModel
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public SubjectType SubjectType { get; set; }

        [Required]
        [MaxLength(10)]
        public string? ExternalId { get; set; }

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
        public string? C_1 { get; set; }

        [MaxLength(64)]
        public string? C_2 { get; set; }

        [MaxLength(64)]
        public string? DQB1_1 { get; set; }

        [MaxLength(64)]
        public string? DQB1_2 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1_1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1_2 { get; set; }
    }

    public static class SubjectInfoBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<SubjectInfo> modelBuilder)
        {
            modelBuilder
                .HasIndex(x => new { x.ExternalId })
                .IsUnique();
        }
    }

    public static class SubjectInfoExtensions
    {
        public static PhenotypeInfo<string> ToPhenotypeInfo(this SubjectInfo subject)
        {
            return new PhenotypeInfo<string>(
                valueA: new LocusInfo<string>(subject.A_1, subject.A_2),
                valueB: new LocusInfo<string>(subject.B_1, subject.B_2),
                valueC: new LocusInfo<string>(subject.C_1, subject.C_2),
                valueDqb1: new LocusInfo<string>(subject.DQB1_1, subject.DQB1_2),
                valueDrb1: new LocusInfo<string>(subject.DRB1_1, subject.DRB1_2));
        }
    }
}
