using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.GeneticData;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness
{
    // ReSharper disable InconsistentNaming

    /// <summary>
    /// Record of masking that was performed when generating the test harness of ID <see cref="TestHarness_Id"/>.
    /// The schema for this data is not fully normalised as the intent is to simply record past requests;
    /// there is no requirement to model the data in a way that permits complex querying.
    /// </summary>
    public class MaskingRecord
    {
        public int Id { get; set; }

        public int TestHarness_Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public TestIndividualCategory TestIndividualCategory { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public Locus Locus { get; set; }

        /// <summary>
        /// Data is stored as a serialised version of the original masking instructions.
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string MaskingRequests { get; set; }
    }

    internal static class MaskingRecordBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<MaskingRecord> modelBuilder)
        {
            modelBuilder
                .HasOne<TestHarness>()
                .WithMany()
                .HasForeignKey(s => s.TestHarness_Id);
        }
    }
}
