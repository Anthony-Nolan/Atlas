using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

namespace Atlas.MatchingAlgorithm.Data.Models.Precompute;

/// <summary>
/// The natural key of a <see cref="SubjectGenotypeSetValue"/>, and therefore the unit of de-duplication: one row per
/// distinct value of this, however many donors share it.
/// </summary>
/// <remarks>
/// A <c>readonly record struct</c> because it is used as a dictionary key by the millions - value equality and a
/// value hash with no allocation. <c>HlaTypingKey</c> is compared ordinally by the generated equality, which matches
/// the column's own lower-case-hex domain (see <c>SubjectGenotypeSetKeyGenerator</c> for why hex and not Base64).
/// </remarks>
public readonly record struct SubjectGenotypeSetKey(string HlaTypingKey, int HaplotypeFrequencySetId, AllowedLociKey AllowedLociKey);

/// <summary>
/// A payload offered for storage. <see cref="SubjectGenotypeSetData"/> is null exactly when
/// <see cref="IsUnrepresented"/> is true, which is the caller's policy, not the encoder's - the format can encode an
/// unrepresented set perfectly well.
/// </summary>
public sealed record SubjectGenotypeSetValueToStore(SubjectGenotypeSetKey Key, bool IsUnrepresented, byte[] SubjectGenotypeSetData);

/// <summary>
/// How a write of values makes sure that each key is stored once. The unique index on the key is what guarantees it in
/// both modes; the modes differ in how a race of two writers on one new key ends.
/// </summary>
public enum SubjectGenotypeSetValueInsertMode
{
    /// <summary>
    /// The insert first looks for each key under <c>UPDLOCK, HOLDLOCK</c>, so a racing writer waits rather than inserts
    /// the same key. For writers that often share keys: the donor import, search and the debug endpoint.
    /// </summary>
    /// <remarks>
    /// The locks are key-range locks on the unique index, and a writer of a new key also locks the gap around it. So it
    /// does not suit many writers of different new keys at once: they wait for each other, and can deadlock (ATL-473).
    /// </remarks>
    KeyRangeLocked,

    /// <summary>
    /// The insert looks for each key without lock hints, and inserts the keys it did not find. A writer that loses a race
    /// on a new key gets a unique index violation, and runs the insert again, which then finds the key. No key-range
    /// locks. For the donor genotype precomputation stage of a data refresh, whose writers almost never share a key: each
    /// batch has its own groups.
    /// </summary>
    RetryOnDuplicate
}

/// <summary>One <c>DonorSubjectGenotypeSets</c> row: which stored value this donor uses at this locus combination.</summary>
public sealed record DonorSubjectGenotypeSetAssignment(int DonorId, AllowedLociKey AllowedLociKey, int SubjectGenotypeSetValueId);

/// <summary>
/// A stored value read by its key, with no donor: a patient's set (ATL-426). <see cref="SubjectGenotypeSetData"/> is
/// null exactly when <see cref="IsUnrepresented"/> is true.
/// </summary>
public sealed record StoredSubjectGenotypeSetValue(bool IsUnrepresented, byte[] SubjectGenotypeSetData);

/// <summary>
/// A donor's stored genotype set at one locus combination, as search reads it. <see cref="SubjectGenotypeSetData"/> is
/// null exactly when <see cref="IsUnrepresented"/> is true.
/// </summary>
/// <param name="HlaTypingKey">The typing key of the stored value, so the reader can check it was computed from the typing the search has.</param>
public sealed record StoredDonorSubjectGenotypeSet(
    int DonorId,
    string HlaTypingKey,
    int HaplotypeFrequencySetId,
    bool IsUnrepresented,
    byte[] SubjectGenotypeSetData);

/// <summary>
/// An assignment to write only while the donor in <c>Donors</c> still has the typing, registry code and ethnicity code
/// that the value was computed from. Only the match prediction loci are compared - DPB1 does not change the set. The
/// registry and ethnicity codes choose the frequency set, so a change to either can change the set too.
/// </summary>
public sealed record GuardedDonorAssignment(
    DonorSubjectGenotypeSetAssignment Assignment,
    PhenotypeInfo<string> ExpectedHla,
    string ExpectedRegistryCode,
    string ExpectedEthnicityCode);

/// <summary>The outcome of <c>UpsertDonorAssignmentsWhereDonorUnchanged</c>, in assignments.</summary>
/// <param name="UpsertedCount">Assignments whose donor was unchanged, and which now point at the given value.</param>
/// <param name="SkippedDonorChangedCount">
/// Assignments not written, because the donor's typing, registry code or ethnicity code has changed, or the donor is gone.
/// </param>
public sealed record GuardedUpsertResult(int UpsertedCount, int SkippedDonorChangedCount);
