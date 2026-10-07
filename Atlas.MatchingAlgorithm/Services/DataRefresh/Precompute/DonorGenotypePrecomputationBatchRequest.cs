#nullable enable

using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// The requests-topic message for one batch of the donor genotype precomputation stage of the data refresh. A claim
/// check: it names the batch and nothing else, and the worker reads the work from the batch row.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thin on purpose.</b> The topic is on the Standard tier, which caps a message at 256 KB. A batch's values would not
/// fit, and its group ids would make a message of each batch as large as the batch.
/// </para>
///
/// <para>
/// <b>No database.</b> The database is on the refresh record. The worker reads it once, when it starts (see
/// <see cref="DonorGenotypePrecomputationTarget"/>), and the dead-letter trigger reads it from the record that the
/// message names.
/// </para>
///
/// <para>
/// <b>Internal to this repository, not a versioned client model</b>, but old and new code can see each other's messages
/// during a rolling deploy. Only add fields; do not rename or remove one.
/// </para>
/// </remarks>
public class DonorGenotypePrecomputationBatchRequest
{
    public int DataRefreshRecordId { get; set; }

    public int RunId { get; set; }

    public int BatchId { get; set; }

    /// <summary>The request that a message body holds.</summary>
    /// <returns>Null when the body is not JSON of a request, or names no batch.</returns>
    public static DonorGenotypePrecomputationBatchRequest? FromBody(string body)
    {
        DonorGenotypePrecomputationBatchRequest? request;
        try
        {
            request = JsonConvert.DeserializeObject<DonorGenotypePrecomputationBatchRequest>(body);
        }
        catch (JsonException)
        {
            return null;
        }

        // A body with no ids reads as zeros, and no batch has them.
        return request is { DataRefreshRecordId: > 0, RunId: > 0, BatchId: > 0 } ? request : null;
    }
}
