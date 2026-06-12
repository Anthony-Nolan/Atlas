// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Data.Models
{
    /// <summary>
    /// Minimal projection of <see cref="HaplotypeFrequency"/> used when loading a whole set into memory.
    /// Carries only the columns needed to build the haplotype frequency cache, avoiding the per-row
    /// <see cref="HaplotypeFrequency.Hla"/> <c>LociInfo</c> allocation of the full entity.
    /// </summary>
    public record LightweightHaplotypeFrequencyRecord
    {
        public string A { get; init; }
        public string B { get; init; }
        public string C { get; init; }
        public string DQB1 { get; init; }
        public string DRB1 { get; init; }
        public int SetId { get; init; }
        public decimal Frequency { get; init; }
        public HaplotypeTypingCategory TypingCategory { get; init; }
    }
}
