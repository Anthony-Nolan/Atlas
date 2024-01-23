using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming

    public abstract class MatchProbability : IBulkInsertModel
    {
        public int Id { get; set; }

        /// <summary>
        /// This will be null for "cross-loci" predictions, e.g., P(x/10), etc.
        /// </summary>
        [Column(TypeName = "nvarchar(10)")]
        public Locus? Locus { get; set; }

        public int MismatchCount { get; set; }

        /// <summary>
        /// Nullable for when patient and/or donor were non-represented.
        /// </summary>
        [Column(TypeName = "decimal(6,5)")]
        public decimal? Probability { get; set; }

        /// <summary>
        /// Nullable for when patient and/or donor were non-represented.
        /// </summary>
        public int? ProbabilityAsPercentage { get; set; }
    }
}
