using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings;

public class HaplotypeFrequencySetCacheSettings
{
    [Range(1, int.MaxValue)]
    public int ActiveSetCacheExpiryMinutes { get; set; }
}