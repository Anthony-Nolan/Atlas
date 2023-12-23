using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming
    public class LocusMatchDetails : IBulkInsertModel
    {
        public int Id { get; set; }
        public int MatchedDonor_Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public Locus Locus { get; set; }

        public int? MatchCount { get; set; }
        
        [Column(TypeName = "nvarchar(128)")]
        public MatchGrade? MatchGrade_1 { get; set; }

        [Column(TypeName = "nvarchar(128)")]
        public MatchGrade? MatchGrade_2 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public MatchConfidence? MatchConfidence_1 { get; set; }

        [Column(TypeName = "nvarchar(32)")]
        public MatchConfidence? MatchConfidence_2 { get; set; }

        public bool? IsAntigenMatch_1 { get; set; }
        public bool? IsAntigenMatch_2 { get; set; }
    }

    public static class LocusMatchCountBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<LocusMatchDetails> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchedDonor>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonor_Id);

            modelBuilder.HasIndex(r => new { r.MatchedDonor_Id, r.Locus, r.MatchCount });
        }
    }
}