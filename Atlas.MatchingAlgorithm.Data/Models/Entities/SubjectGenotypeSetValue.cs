using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// De-duplicated, pre-computed subject-genotype-set payload, stored once per distinct
    /// (<see cref="HlaTypingKey"/>, <see cref="HaplotypeFrequencySetId"/>,
    /// <see cref="MatchingAlgorithmHlaNomenclatureVersion"/>, <see cref="AllowedLociKey"/>) — no matter how many
    /// donors share it. Referenced by <see cref="DonorSubjectGenotypeSet"/>. Populated by the precompute service
    /// (ATL-272); ships without a caller.
    /// </summary>
    public class SubjectGenotypeSetValue
    {
        public int Id { get; set; }

        /// <summary>
        /// Canonical, deterministic, collision-safe representation (e.g. a hash) of the donor's compressed HLA
        /// typing; the exact encoding is owned by the precompute service (ATL-272). Part of the row identity, so it
        /// must be a bounded length that fits in the unique index (not nvarchar(max)).
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string HlaTypingKey { get; set; }

        public AllowedLociKey AllowedLociKey { get; set; }

        /// <summary>
        /// The haplotype frequency set the values were computed against. Logical reference to the HF set (which lives
        /// in the Match Prediction database) — not a DB-level FK.
        /// </summary>
        public int HaplotypeFrequencySetId { get; set; }

        /// <summary>
        /// The matching-algorithm HLA nomenclature version active when the P-group conversion ran. Part of the row
        /// identity because it changes on every Data Refresh independently of the donor's typing and HF set, so two
        /// computations of the same typing+HF set under different versions must not collide.
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        /// <summary>
        /// True when this typing has zero matching haplotypes in the HF set for this <see cref="AllowedLociKey"/>;
        /// <see cref="SubjectGenotypeSetData"/> is null when true.
        /// </summary>
        public bool IsUnrepresented { get; set; }

        /// <summary>
        /// The fully-converted, match-ready payload — gzip-compressed, pool-encoded binary of
        /// <c>Atlas.MatchPrediction.Models.SubjectGenotypeSet</c>. Null when <see cref="IsUnrepresented"/> is true.
        /// </summary>
        [Column(TypeName = "varbinary(max)")]
        public byte[] SubjectGenotypeSetData { get; set; }
    }
}
