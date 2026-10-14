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

    /// <summary>One entry for the typing, or one entry for each donor code.</summary>
    public List<ComputedSubject> Subjects { get; set; }

    public static ComputeGenotypeSetsResponse From(GenotypeSetDebugResult result) => new()
    {
        Database = result.Database,
        HlaNomenclatureVersion = result.HlaNomenclatureVersion,
        Subjects = result.Subjects.Select(ComputedSubject.From).ToList()
    };
}
