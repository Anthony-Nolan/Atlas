using Atlas.MatchingAlgorithm.Services.Debug.Precompute;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The haplotype frequency set that a genotype set was computed with.</summary>
internal class ComputedFrequencySet
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string RegistryCode { get; set; }

    public string EthnicityCode { get; set; }

    public string HlaNomenclatureVersion { get; set; }

    /// <summary>True when search would choose this set now, for the set's own registry and ethnicity codes.</summary>
    public bool IsActive { get; set; }

    public static ComputedFrequencySet From(HaplotypeFrequencySetUsed set) => set == null
        ? null
        : new ComputedFrequencySet
        {
            Id = set.Id,
            Name = set.Name,
            RegistryCode = set.RegistryCode,
            EthnicityCode = set.EthnicityCode,
            HlaNomenclatureVersion = set.HlaNomenclatureVersion,
            IsActive = set.IsActive
        };
}
