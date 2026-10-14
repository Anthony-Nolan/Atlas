using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The value row of a genotype set that the request saved.</summary>
internal class SavedValue
{
    public int SubjectGenotypeSetValueId { get; set; }

    /// <summary><c>Created</c> when this request stored the row; <c>AlreadyExisted</c> when it was stored before, and is kept as it is.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SavedValueOutcome Outcome { get; set; }

    public static SavedValue From(SavedGenotypeSetValue value) => value == null
        ? null
        : new SavedValue { SubjectGenotypeSetValueId = value.SubjectGenotypeSetValueId, Outcome = value.Outcome };
}
