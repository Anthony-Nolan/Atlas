using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The genotype set of one subject at one locus combination.</summary>
internal class ComputedGenotypeSet
{
    [JsonConverter(typeof(StringEnumConverter))]
    public AllowedLociKey AllowedLociKey { get; set; }

    /// <summary>The typing key of the value row, as the pre-computation builds it. Use it to find the row with SQL.</summary>
    public string HlaTypingKey { get; set; }

    public ComputedFrequencySet HaplotypeFrequencySet { get; set; }

    /// <summary>Not given when the set could not be computed.</summary>
    public bool? IsUnrepresented { get; set; }

    /// <summary>Not given when the set could not be computed.</summary>
    public int? GenotypeCount { get; set; }

    /// <summary>Not given when the set could not be computed.</summary>
    public decimal? SumOfLikelihoods { get; set; }

    /// <summary>The size of the stored data. Not given for an unrepresented set, as nothing is stored for it.</summary>
    public int? PayloadSizeBytes { get; set; }

    /// <summary>The genotypes of the set, when the request includes them.</summary>
    public List<ComputedGenotype> Genotypes { get; set; }

    /// <summary>The value row, when the request saves.</summary>
    public SavedValue Saved { get; set; }

    /// <summary>Why the set could not be computed.</summary>
    public string Error { get; set; }

    public static ComputedGenotypeSet From(GenotypeSetDebugItem item)
    {
        var computed = item.Error == null;

        return new ComputedGenotypeSet
        {
            AllowedLociKey = item.AllowedLociKey,
            HlaTypingKey = item.HlaTypingKey,
            HaplotypeFrequencySet = ComputedFrequencySet.From(item.HaplotypeFrequencySet),
            IsUnrepresented = computed ? item.IsUnrepresented : null,
            GenotypeCount = computed ? item.GenotypeCount : null,
            SumOfLikelihoods = computed ? item.SumOfLikelihoods : null,
            PayloadSizeBytes = item.PayloadSizeBytes,
            Genotypes = item.Genotypes?.Select(ComputedGenotype.From).ToList(),
            Saved = SavedValue.From(item.Saved),
            Error = item.Error
        };
    }
}
