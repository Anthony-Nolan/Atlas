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

/// <summary>One <c>DonorSubjectGenotypeSets</c> row: which stored value this donor uses at this locus combination.</summary>
public sealed record DonorSubjectGenotypeSetAssignment(int DonorId, AllowedLociKey AllowedLociKey, int SubjectGenotypeSetValueId);

/// <summary>
/// A donor's stored genotype set at one locus combination, as search reads it. <see cref="SubjectGenotypeSetData"/> is
/// null exactly when <see cref="IsUnrepresented"/> is true.
/// </summary>
public sealed record StoredDonorSubjectGenotypeSet(int DonorId, int HaplotypeFrequencySetId, bool IsUnrepresented, byte[] SubjectGenotypeSetData);

/// <summary>
/// An assignment to write only while the donor's typing in <c>Donors</c> is still <see cref="ExpectedHla"/>: the typing
/// the value was computed from. Only the match prediction loci are compared - DPB1 does not change the set.
/// </summary>
public sealed record TypingGuardedDonorAssignment(DonorSubjectGenotypeSetAssignment Assignment, PhenotypeInfo<string> ExpectedHla);

/// <summary>The outcome of <c>UpsertDonorAssignmentsWhereTypingUnchanged</c>, in assignments.</summary>
/// <param name="UpsertedCount">Assignments whose donor's typing was unchanged, and which now point at the given value.</param>
/// <param name="SkippedTypingChangedCount">Assignments not written, because the donor's typing has changed or the donor is gone.</param>
public sealed record TypingGuardedUpsertResult(int UpsertedCount, int SkippedTypingChangedCount);
