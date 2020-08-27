using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Verification
{
    /// <summary>
    /// Parent entity tying together all search requests and results generated from a single
    /// verification run.
    /// </summary>
    public class VerificationRun : ParentEntityBase
    {
        public int TestHarness_Id { get; set; }
        public int SearchLociCount { get; set; }
        
        /// <summary>
        /// Serialised copy of the <see cref="Client.Models.Search.Requests.SearchRequest"/> submitted for all patients
        /// within this run, minus the SearchHla.
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string SearchRequest { get; set; }

        public bool SearchRequestsSubmitted { get; set; }
    }
    
    internal static class VerificationRunBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<VerificationRun> modelBuilder)
        {
            modelBuilder
                .Property(t => t.CreatedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder
                .HasOne<TestHarness.TestHarness>()
                .WithMany()
                .HasForeignKey(t => t.TestHarness_Id);
        }
    }
}
