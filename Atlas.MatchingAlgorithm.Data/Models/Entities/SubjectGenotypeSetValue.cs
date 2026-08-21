using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// De-duplicated, pre-computed subject-genotype-set payload, stored once per distinct
    /// (<see cref="HlaTypingKey"/>, <see cref="HaplotypeFrequencySetId"/>, <see cref="AllowedLociKey"/>) — no matter
    /// how many donors share it. Referenced by <see cref="DonorSubjectGenotypeSet"/>. Populated by the precompute
    /// service; no caller reads it yet.
    /// </summary>
    public class SubjectGenotypeSetValue
    {
        public int Id { get; set; }

        /// <summary>
        /// A fixed-length code representing the donor's HLA typing, used to spot when two donors share the same
        /// typing so we only store the payload once. Typings themselves vary too much in length to index directly,
        /// so this is a short, consistent stand-in for one instead (sized here for a SHA-256 hex digest). The
        /// precompute service decides exactly how it's generated, and must generate it the same way every time.
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
