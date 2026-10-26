namespace Atlas.MatchPrediction.Data.Models;

/// <summary>The numbers of haplotype frequency sets and of their distinct content hashes.</summary>
/// <param name="Sets">All the sets, active or not.</param>
/// <param name="SetsWithNoContentHash">The sets that have no content hash yet.</param>
/// <param name="ContentHashesOfActiveSets">The distinct content hashes of the active sets: the contents in use.</param>
/// <param name="ContentHashes">The distinct content hashes of all the sets.</param>
public record HaplotypeFrequencySetContentHashCounts(int Sets, int SetsWithNoContentHash, int ContentHashesOfActiveSets, int ContentHashes);
