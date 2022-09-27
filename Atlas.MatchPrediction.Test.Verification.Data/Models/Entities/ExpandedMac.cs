using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities
{
    public class ExpandedMac : IBulkInsertModel
    {
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public string SecondField { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public string Code { get; set; }
    }

    internal static class ExpandedMacBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<ExpandedMac> modelBuilder)
        {
            modelBuilder
                .HasIndex(x => new { x.Code, x.SecondField })
                .IsUnique();
        }
    }
}
