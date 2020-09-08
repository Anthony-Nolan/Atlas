using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness
{
    public class NormalisedPool : ParentEntityBase
    {
        /// <summary>
        /// Name/address of the sql server which hosted the Match Prediction database,
        /// from which haplotypes were retrieved.
        /// This is useful to keep track of the environment used for test harness generation.
        /// </summary>
        [MaxLength(200)]
        public string HaplotypeFrequenciesDataSource { get; set; }

        /// <summary>
        /// Id of the haplotype frequency set that was used to generate the normalised pool.
        /// </summary>
        public int HaplotypeFrequencySetId { get; set; }
    }

    internal static class NormalisedPoolBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<NormalisedPool> modelBuilder)
        {
            modelBuilder
                .Property(t => t.CreatedDateTime)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
