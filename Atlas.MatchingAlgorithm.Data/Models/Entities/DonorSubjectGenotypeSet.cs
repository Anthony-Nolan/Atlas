namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// Thin per-donor mapping row: points a donor's (<see cref="DonorId"/>, <see cref="AllowedLociKey"/>) at the
    /// de-duplicated <see cref="SubjectGenotypeSetValue"/> it resolves to. One row per (DonorId, AllowedLociKey) —
    /// at most 4 rows per donor. Populated by the precompute service (ATL-272); ships without a caller.
    /// </summary>
    public class DonorSubjectGenotypeSet
    {
        public int Id { get; set; }

        /// <summary>
        /// The Atlas donor id (<c>Donors.DonorId</c>), NOT the <c>Donors.Id</c> identity PK — same key the
        /// <c>MatchingHlaAt*</c> tables use and that search joins on (<c>m.DonorId = d.DonorId</c>).
        /// No DB-level FK, so the writer (ATL-272) must populate it with the correct <c>Donors.DonorId</c>.
        /// </summary>
        public int DonorId { get; set; }

        public AllowedLociKey AllowedLociKey { get; set; }

        /// <summary>
        /// Logical reference to <see cref="SubjectGenotypeSetValue.Id"/>. Not a DB-level FK — the matching transient
        /// databases deliberately hold no foreign key constraints (mirrors the <c>MatchingHlaAt*</c> → <c>HlaNames</c>
        /// normalisation), so the writer (ATL-272) is responsible for referential integrity.
        /// </summary>
        public int SubjectGenotypeSetValueId { get; set; }
    }
}
