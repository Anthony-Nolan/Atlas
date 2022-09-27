using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData;
using Atlas.Common.Sql.BulkInsert;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification
{
    // ReSharper disable InconsistentNaming

    public class LocusMatchCount : IBulkInsertModel
    {
        public int Id { get; set; }
        public int MatchedDonor_Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public Locus Locus { get; set; }

        public int? MatchCount { get; set; }
    }

    internal static class LocusMatchCountBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<LocusMatchCount> modelBuilder)
        {
            modelBuilder
                .HasOne<MatchedDonor>()
                .WithMany()
                .HasForeignKey(r => r.MatchedDonor_Id);

            modelBuilder.HasIndex(r => new {r.MatchedDonor_Id, r.Locus, r.MatchCount});
        }
    }
}
