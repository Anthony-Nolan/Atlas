#nullable enable

using System;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>
/// A request to the genotype set compute endpoint: a subject, the haplotype frequency set to impute it against, and the
/// locus combinations to compute.
/// </summary>
public sealed record GenotypeSetDebugRequest
{
    /// <summary>A typing to compute, for example a patient's. Give this or <see cref="ExternalDonorCodes"/>, not both.</summary>
    public PhenotypeInfo<string>? HlaTyping { get; init; }

    /// <summary>Donors to compute. Their typing and their registry and ethnicity codes are read from the matching database.</summary>
    public IReadOnlyCollection<string>? ExternalDonorCodes { get; init; }

    /// <summary>
    /// For a typing only: the registry and ethnicity codes that choose the frequency set, with the rules of search. Null
    /// chooses the global set. A donor's set is chosen from the donor's own codes.
    /// </summary>
    public FrequencySetMetadata? FrequencySetMetadata { get; init; }

    /// <summary>Exactly this frequency set, active or not, in place of the set that the codes choose.</summary>
    public int? HaplotypeFrequencySetId { get; init; }

    /// <summary>The combinations to compute. Null or empty computes all four.</summary>
    public IReadOnlyCollection<AllowedLociKey>? AllowedLociKeys { get; init; }

    /// <summary>Keeps the genotypes of each set in the result. Without it, only their count is kept.</summary>
    public bool IncludeGenotypes { get; init; }

    /// <summary>
    /// Stores what is computed in the active database: the value rows, and for a donor also its link rows. Allowed only
    /// when <see cref="DebugSettings.AllowPrecomputeWrites"/> is true.
    /// </summary>
    public bool Save { get; init; }
}

/// <summary>The result of a request.</summary>
/// <param name="Database">The transient database that was read, and written when the request saves.</param>
/// <param name="HlaNomenclatureVersion">The nomenclature version of that database, at which the P group conversion ran.</param>
/// <param name="Subjects">One entry for the typing, or one entry for each donor code.</param>
public sealed record GenotypeSetDebugResult(
    TransientDatabase Database,
    string HlaNomenclatureVersion,
    IReadOnlyList<GenotypeSetDebugSubject> Subjects);

/// <summary>One subject of a request: the typing of the request, or one donor.</summary>
public sealed record GenotypeSetDebugSubject
{
    /// <summary>The code of the donor; null for a typing.</summary>
    public string? ExternalDonorCode { get; init; }

    public int? DonorId { get; init; }

    /// <summary>False when no donor has the code. Then nothing more is set.</summary>
    public bool Found { get; init; }

    public bool? IsAvailableForSearch { get; init; }

    public string? RegistryCode { get; init; }

    public string? EthnicityCode { get; init; }

    /// <summary>The typing that was computed: the typing of the request, or the donor's typing in the database.</summary>
    public PhenotypeInfo<string>? HlaTyping { get; init; }

    /// <summary>
    /// For a donor of a request that saves: true when the donor's link rows were written. They are written only when all
    /// the requested values of the donor are stored.
    /// </summary>
    public bool? LinkRowsWritten { get; init; }

    /// <summary>Why the subject could not be computed, or saved; null when nothing failed for the subject as a whole.</summary>
    public string? Error { get; init; }

    public IReadOnlyList<GenotypeSetDebugItem> GenotypeSets { get; init; } = [];
}

/// <summary>The genotype set of one subject at one locus combination.</summary>
public sealed record GenotypeSetDebugItem
{
    public AllowedLociKey AllowedLociKey { get; init; }

    /// <summary>The typing key of the value row, as the pre-computation builds it.</summary>
    public string HlaTypingKey { get; init; } = "";

    public HaplotypeFrequencySetUsed? HaplotypeFrequencySet { get; init; }

    public bool IsUnrepresented { get; init; }

    public int GenotypeCount { get; init; }

    public decimal SumOfLikelihoods { get; init; }

    /// <summary>The size of the encoded set; null when the set is unrepresented, as nothing is stored for it then.</summary>
    public int? PayloadSizeBytes { get; init; }

    /// <summary>The genotypes of the set, when the request includes them.</summary>
    public IReadOnlyCollection<GenotypeAtDesiredResolutions>? Genotypes { get; init; }

    /// <summary>The value row, when the request saves.</summary>
    public SavedGenotypeSetValue? Saved { get; init; }

    /// <summary>Why the set could not be computed; null when it was computed.</summary>
    public string? Error { get; init; }
}

/// <summary>The haplotype frequency set that a genotype set was computed with.</summary>
/// <param name="IsActive">
/// True when search would choose this set now, for the set's own registry and ethnicity codes. A set that a newer import
/// has replaced is not active.
/// </param>
public sealed record HaplotypeFrequencySetUsed(
    int Id,
    string? Name,
    string? RegistryCode,
    string? EthnicityCode,
    string? HlaNomenclatureVersion,
    bool IsActive);

public enum SavedValueOutcome
{
    /// <summary>This request stored the value row.</summary>
    Created,

    /// <summary>A value row with the same key was stored already, and is kept as it is.</summary>
    AlreadyExisted
}

public sealed record SavedGenotypeSetValue(int SubjectGenotypeSetValueId, SavedValueOutcome Outcome);

/// <summary>A request to save while <see cref="DebugSettings.AllowPrecomputeWrites"/> is false. Nothing was changed.</summary>
public class PrecomputeWritesNotAllowedException()
    : Exception("Saving genotype sets is turned off in this environment (Debug:AllowPrecomputeWrites). Nothing was changed.");

/// <summary>
/// The matching databases are not in a state that the request can use: for example, no data refresh has completed, so no
/// database is active.
/// </summary>
public class GenotypeSetDebugUnavailableException(string message) : Exception(message);
