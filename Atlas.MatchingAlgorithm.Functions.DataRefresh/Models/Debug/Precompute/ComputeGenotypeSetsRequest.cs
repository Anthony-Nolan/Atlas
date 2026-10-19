using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The request body of <c>POST debug/precompute/genotypeSets/compute</c>.</summary>
internal class ComputeGenotypeSetsRequest
{
    /// <summary>A typing to compute, for example a patient's. Give this or <see cref="ExternalDonorCodes"/>, not both.</summary>
    public PhenotypeInfoTransfer<string> HlaTyping { get; set; }

    /// <summary>
    /// At most 20 donors. Their typing and their registry and ethnicity codes are read from the matching database that
    /// <see cref="Database"/> names (the active one by default).
    /// </summary>
    public List<string> ExternalDonorCodes { get; set; }

    /// <summary>
    /// For a typing only: the registry and ethnicity codes that choose the frequency set, with the rules of search. When it
    /// is not given, the global set is used.
    /// </summary>
    public FrequencySetMetadata FrequencySetMetadata { get; set; }

    /// <summary>Exactly this frequency set, active or not, in place of the set that the codes choose.</summary>
    public int? HaplotypeFrequencySetId { get; set; }

    /// <summary>The locus combinations to compute, by name, for example <c>ABDrb1</c>. When none are given, all four are computed.</summary>
    public List<AllowedLociKey> AllowedLociKeys { get; set; }

    /// <summary>Returns each genotype of each set, with its likelihood. The response can be large.</summary>
    public bool IncludeGenotypes { get; set; }

    /// <summary>
    /// Stores the sets in the active matching database: the value rows, and for a donor its link rows. Allowed only when the
    /// app setting <c>Debug:AllowPrecomputeWrites</c> is true.
    /// </summary>
    public bool Save { get; set; }

    /// <summary>
    /// Compares each computed set with its stored row, as search reads it, and gives a parity result for each set. Not with
    /// <c>Save</c>.
    /// </summary>
    public bool CompareWithStored { get; set; }

    /// <summary>
    /// The matching database to read: <c>Active</c> (when it is not given) or <c>Dormant</c>. Data Refresh writes to the
    /// dormant database, so <c>Dormant</c> checks its rows before the databases swap. The sets are then computed at the
    /// nomenclature version of the latest refresh that wrote to it. Not <c>Dormant</c> with <c>Save</c>.
    /// </summary>
    public TransientDatabaseRole? Database { get; set; }

    /// <summary>
    /// With <c>CompareWithStored</c>: the most differences to give for one genotype set, from 1 to 1,000. When it is not
    /// given, 10. All the differences are counted.
    /// </summary>
    public int? MaxDifferencesPerRow { get; set; }

    public GenotypeSetDebugRequest ToGenotypeSetDebugRequest() => new()
    {
        HlaTyping = HlaTyping?.ToPhenotypeInfo(),
        ExternalDonorCodes = ExternalDonorCodes,
        FrequencySetMetadata = FrequencySetMetadata,
        HaplotypeFrequencySetId = HaplotypeFrequencySetId,
        AllowedLociKeys = AllowedLociKeys,
        IncludeGenotypes = IncludeGenotypes,
        Save = Save,
        CompareWithStored = CompareWithStored,
        Database = Database ?? TransientDatabaseRole.Active,
        MaxDifferencesPerRow = MaxDifferencesPerRow ?? GenotypeSetDebugRequest.DefaultMaxDifferencesPerRow
    };
}
