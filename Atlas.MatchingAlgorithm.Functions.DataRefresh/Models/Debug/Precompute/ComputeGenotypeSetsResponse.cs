using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The response body of <c>POST debug/precompute/genotypeSets/compute</c>.</summary>
internal class ComputeGenotypeSetsResponse
{
    /// <summary>The transient database that was read, and written when the request saves.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public TransientDatabase Database { get; set; }

    /// <summary>The nomenclature version of that database, at which the P group conversion ran.</summary>
    public string HlaNomenclatureVersion { get; set; }

    /// <summary>
    /// With <c>CompareWithStored</c>: the number of genotype sets with each parity result. Every result is given, also
    /// when its count is 0.
    /// </summary>
    public Dictionary<string, int> ParityCounts { get; set; }

    /// <summary>With <c>CompareWithStored</c>, for donors: the number of donor codes that no donor has.</summary>
    public int? DonorsNotFound { get; set; }

    /// <summary>
    /// With <c>CompareWithStored</c>: the number of subjects that failed as a whole, so that they have no genotype sets to
    /// compare. Their <c>Error</c> says why.
    /// </summary>
    public int? SubjectErrors { get; set; }

    /// <summary>One entry for the typing, or one entry for each donor code.</summary>
    public List<ComputedSubject> Subjects { get; set; }

    public static ComputeGenotypeSetsResponse From(GenotypeSetDebugResult result) => new()
    {
        Database = result.Database,
        HlaNomenclatureVersion = result.HlaNomenclatureVersion,
        ParityCounts = result.Parity?.ResultCounts.ToDictionary(count => count.Key.ToString(), count => count.Value),
        DonorsNotFound = result.Parity?.DonorsNotFound,
        SubjectErrors = result.Parity?.SubjectErrors,
        Subjects = result.Subjects.Select(ComputedSubject.From).ToList()
    };
}
