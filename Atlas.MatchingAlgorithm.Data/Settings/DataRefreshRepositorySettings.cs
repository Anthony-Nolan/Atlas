namespace Atlas.MatchingAlgorithm.Data.Settings;

/// <summary>
/// The slice of the Data Refresh configuration that the data layer itself needs.
///
/// <para>
/// This exists because <c>DataRefreshSettings</c> lives in Atlas.MatchingAlgorithm, which this project cannot see, and
/// because <c>DataRefreshSettings</c> is only registered by apps that register the refresh - searches and ongoing donor
/// management never do, yet they share these repositories. The composition root projects the relevant values across
/// that boundary, falling back to the historic hard-coded defaults when the refresh settings are absent.
/// </para>
/// </summary>
public class DataRefreshRepositorySettings
{
    /// <summary>The value this was a hard-coded const at, before it became configurable. Behaviour is unchanged when unset.</summary>
    public const int DefaultSqlBulkCopyBatchSize = 10000;

    /// <summary>Rows per <c>SqlBulkCopy</c> batch when writing Donors / MatchingHlaAt&lt;Locus&gt;.</summary>
    public int SqlBulkCopyBatchSize { get; set; } = DefaultSqlBulkCopyBatchSize;
}
