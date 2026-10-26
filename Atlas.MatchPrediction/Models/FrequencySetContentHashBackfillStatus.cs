namespace Atlas.MatchPrediction.Models;

/// <summary>The answer of the content hash backfill endpoint of the haplotype frequency sets.</summary>
/// <param name="Sets">All the sets, active or not.</param>
/// <param name="SetsWithNoContentHash">The sets that have no content hash yet. 0 means the backfill is done.</param>
/// <param name="ContentHashesOfActiveSets">The distinct content hashes of the active sets: the contents in use.</param>
/// <param name="ContentHashes">The distinct content hashes of all the sets.</param>
/// <param name="IsRunning">Whether a run of the backfill goes on in the instance that answered.</param>
public record FrequencySetContentHashBackfillStatus(
    int Sets,
    int SetsWithNoContentHash,
    int ContentHashesOfActiveSets,
    int ContentHashes,
    bool IsRunning);
