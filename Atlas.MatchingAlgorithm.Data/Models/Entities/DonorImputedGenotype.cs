using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// Pre-computed imputed-genotype payload for a donor, per allowed-loci combination.
    /// One row per (<see cref="DonorId"/>, <see cref="AllowedLociKey"/>) — at most 4 rows per donor.
    /// Populated by the precompute service (ATL-272); ships without a caller.
    /// </summary>
    public class DonorImputedGenotype
    {
        public int Id { get; set; }

        /// <summary>
        /// Logical reference to the internal Atlas donor id (<c>Donors.Id</c>). Not a DB-level FK —
        /// the matching transient databases deliberately hold no foreign key constraints.
        /// </summary>
        public int DonorId { get; set; }

        public AllowedLociKey AllowedLociKey { get; set; }

        /// <summary>
        /// True when this donor has zero matching haplotypes in the HF set for this specific
        /// <see cref="AllowedLociKey"/>; <see cref="ImputedGenotypeData"/> is null when true.
        /// </summary>
        public bool IsUnrepresented { get; set; }

        /// <summary>
        /// The truncated imputed genotype/likelihood payload (serialized
        /// <c>Atlas.MatchPrediction.Models.ImputedGenotypes</c>). Null when <see cref="IsUnrepresented"/> is true.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string ImputedGenotypeData { get; set; }

        /// <summary>
        /// The haplotype frequency set the values were computed against. Logical reference to the HF set
        /// (which lives in the Match Prediction database) — not a DB-level FK.
        /// </summary>
        public int HaplotypeFrequencySetId { get; set; }
    }
}
