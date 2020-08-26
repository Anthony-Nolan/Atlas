using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class ExpandedMac : IModel
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
