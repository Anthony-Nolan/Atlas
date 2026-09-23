using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Repositories.Precompute;

public interface ISubjectGenotypeSetRepository
{
    /// <summary>
    /// The ids of the keys that are already stored. Keys with no row are simply absent from the result.
    /// </summary>
    /// <remarks>
    /// Purely an optimisation for the caller: it lets an expensive imputation be skipped for a typing that some
    /// earlier batch, or some earlier refresh, has already paid for. It is NOT how duplicate rows are prevented -
    /// that is <see cref="GetOrCreateValueIds"/>'s guarded insert - so a caller may skip this call entirely and still
    /// be correct, just slower.
    /// </remarks>
    Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetExistingValueIds(IReadOnlyCollection<SubjectGenotypeSetKey> keys);

    /// <summary>
    /// Stores every value whose key is not stored yet, and returns the id of every key passed in - whether this call
    /// created it, an earlier call created it, or a concurrent worker created it.
    /// </summary>
    /// <remarks>
    /// Existing rows are left exactly as they are. The key is content-derived, so a stored payload and a freshly
    /// computed one for the same key describe the same genotype set; rewriting would cost a large
    /// <c>varbinary(max)</c> update to store what is already there.
    /// </remarks>
    Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetOrCreateValueIds(IReadOnlyCollection<SubjectGenotypeSetValueToStore> values);

    /// <summary>
    /// Writes the per-donor mapping rows pointing donors at the values they resolve to.
    /// </summary>
    /// <remarks>
    /// A plain insert, and all or nothing. <c>IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey</c> is unique, so
    /// writing a donor that already has a row for that combination throws - which is correct while the only caller is
    /// a refresh running against freshly cleared tables. When the write throws, it keeps no row, so the caller can send
    /// the same assignments again.
    /// </remarks>
    Task WriteDonorAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments);
}

/// <summary>
/// Reads and writes the two precompute tables of the transient (A/B) matching database.
///
/// <para>
/// <b>No in-memory key-to-id cache</b>, deliberately unlike <see cref="HlaNamesRepository"/>. That table holds a few
/// hundred thousand short strings and every donor reuses them; this one holds millions of rows with large payloads,
/// typing keys repeat far less, and a cache would survive a swap of the transient database and hand out ids belonging
/// to the other one. <c>GetOrCreateValueIds_AcrossSeparateRepositoryInstances_ReturnsTheSameIds</c> is the integration
/// test that fails if a cache is reintroduced.
/// </para>
///
/// <para>
/// <b>No transactions.</b> Each write is its own autocommit statement, as everywhere else in this project. The
/// orchestrator's ordering - values first, then donor assignments - is what makes a crash between the two recoverable,
/// not a scope around them.
/// </para>
/// </summary>
public class SubjectGenotypeSetRepository : Repository, ISubjectGenotypeSetRepository
{
    private const string ValuesTableName = "SubjectGenotypeSetValues";
    private const string AssignmentsTableName = "DonorSubjectGenotypeSets";
    private const string StagingTableName = "#StagedGenotypeSetValues";

    private const int CommandTimeoutInSeconds = 600;

    /// <summary>
    /// Staged rows per round trip.
    ///
    /// <para>
    /// Bounded for two reasons, and neither is a guess about a "good batch size". SQL Server escalates to a table lock
    /// at roughly 5,000 locks on one statement, and the guarded insert takes key-range locks per probed key - so a
    /// single statement probing a donor batch's worth of keys (2,000 donors x 4 combinations = 8,000) would turn
    /// <c>HOLDLOCK</c> into a table lock and serialise every writer. It also bounds tempdb, which matters when the
    /// staged rows carry <c>varbinary(max)</c> payloads.
    /// </para>
    /// </summary>
    internal const int StagingChunkSize = 1000;

    /// <summary>
    /// Transient-failure attempts for one chunk's insert and read-back.
    ///
    /// <para>
    /// A retry re-runs against the temp table that is already populated, and the insert is guarded by
    /// <c>NOT EXISTS</c>, so an attempt that partially succeeded is picked up rather than repeated.
    /// </para>
    ///
    /// <para>
    /// A backstop, not the mechanism. <see cref="InsertMissingValuesSql"/>'s lock hints are what stop the collision
    /// happening; measured with 8 concurrent writers on one key, this retry never fires while they are in place. It
    /// exists for the deadlock a longer chunk can still produce, and so that one lost race costs a second attempt
    /// rather than a failed refresh stage.
    /// </para>
    /// </summary>
    private const int MaxAttempts = 3;

    /// <summary>Deadlock victim, and the two unique-constraint violations a lost race would surface as.</summary>
    private static readonly HashSet<int> RetryableErrorNumbers = [1205, 2601, 2627];

    private static readonly string[] AssignmentColumnNames =
    [
        "Id",
        nameof(DonorSubjectGenotypeSet.DonorId),
        nameof(DonorSubjectGenotypeSet.AllowedLociKey),
        nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)
    ];

    private const string TruncateStagingTableSql = $"TRUNCATE TABLE {StagingTableName}";

    /// <summary>
    /// <c>COLLATE DATABASE_DEFAULT</c> is not optional: a temp table otherwise takes tempdb's collation, and joining
    /// an <c>nvarchar</c> column across two collations is an error rather than a slow query.
    /// </summary>
    private const string CreateStagingTableSql = $"""
        CREATE TABLE {StagingTableName} (
            {nameof(SubjectGenotypeSetValue.HlaTypingKey)}            nvarchar(64)   COLLATE DATABASE_DEFAULT NOT NULL,
            {nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)} int            NOT NULL,
            {nameof(SubjectGenotypeSetValue.AllowedLociKey)}          nvarchar(16)   COLLATE DATABASE_DEFAULT NOT NULL,
            {nameof(SubjectGenotypeSetValue.IsUnrepresented)}         bit            NOT NULL,
            {nameof(SubjectGenotypeSetValue.SubjectGenotypeSetData)}  varbinary(max) NULL,
            PRIMARY KEY (
                {nameof(SubjectGenotypeSetValue.HlaTypingKey)},
                {nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)},
                {nameof(SubjectGenotypeSetValue.AllowedLociKey)}))
        """;

    /// <summary>
    /// Inserts the staged keys that are not stored yet.
    ///
    /// <para>
    /// <b><c>UPDLOCK, HOLDLOCK</c> is what makes this safe under concurrency.</b> The hints take key-range update
    /// locks on the unique index for each probed key, held to the end of the statement - an autocommit statement is
    /// itself a transaction, so no explicit <c>TransactionScope</c> is needed. A racing worker probing the same key
    /// blocks on the probe instead of passing <c>NOT EXISTS</c> and inserting a duplicate.
    /// </para>
    ///
    /// <para>
    /// <b>Measured, not assumed.</b> Removing the hints and disabling the retry makes 8 concurrent writers on one new
    /// key collide on the unique index on every run of
    /// <c>GetOrCreateValueIds_ConcurrentlyForTheSameNewKey_StoresOneRowAndAgreesOnItsId</c>. Keeping either one alone
    /// makes that test pass, which is why the test cannot be read as pinning these hints: it pins the outcome, and
    /// the outcome survives losing one of the two. What the hints buy over the retry alone is that a payload-heavy
    /// set-based insert is never rolled back and re-run under contention.
    /// </para>
    /// </summary>
    private const string InsertMissingValuesSql = $"""
        INSERT INTO {ValuesTableName} (
            {nameof(SubjectGenotypeSetValue.HlaTypingKey)},
            {nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)},
            {nameof(SubjectGenotypeSetValue.AllowedLociKey)},
            {nameof(SubjectGenotypeSetValue.IsUnrepresented)},
            {nameof(SubjectGenotypeSetValue.SubjectGenotypeSetData)})
        SELECT
            s.{nameof(SubjectGenotypeSetValue.HlaTypingKey)},
            s.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)},
            s.{nameof(SubjectGenotypeSetValue.AllowedLociKey)},
            s.{nameof(SubjectGenotypeSetValue.IsUnrepresented)},
            s.{nameof(SubjectGenotypeSetValue.SubjectGenotypeSetData)}
        FROM {StagingTableName} s
        WHERE NOT EXISTS (
            SELECT 1 FROM {ValuesTableName} v WITH (UPDLOCK, HOLDLOCK)
            WHERE v.{nameof(SubjectGenotypeSetValue.HlaTypingKey)} = s.{nameof(SubjectGenotypeSetValue.HlaTypingKey)}
              AND v.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)} = s.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)}
              AND v.{nameof(SubjectGenotypeSetValue.AllowedLociKey)} = s.{nameof(SubjectGenotypeSetValue.AllowedLociKey)})
        """;

    /// <summary>
    /// Recovers the id of every staged key.
    ///
    /// <para>
    /// A join back rather than <c>OUTPUT INSERTED.*</c>: it runs after the insert has committed, so it returns ids for
    /// the keys this call created AND for keys a concurrent worker created while this call was blocked on the probe.
    /// <c>OUTPUT</c> would only ever report the former, and the caller needs both.
    /// </para>
    /// </summary>
    private const string SelectStagedValueIdsSql = $"""
        SELECT
            v.{nameof(SubjectGenotypeSetValue.Id)},
            v.{nameof(SubjectGenotypeSetValue.HlaTypingKey)},
            v.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)},
            v.{nameof(SubjectGenotypeSetValue.AllowedLociKey)}
        FROM {ValuesTableName} v
        INNER JOIN {StagingTableName} s
            ON v.{nameof(SubjectGenotypeSetValue.HlaTypingKey)} = s.{nameof(SubjectGenotypeSetValue.HlaTypingKey)}
           AND v.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)} = s.{nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId)}
           AND v.{nameof(SubjectGenotypeSetValue.AllowedLociKey)} = s.{nameof(SubjectGenotypeSetValue.AllowedLociKey)}
        """;

    public SubjectGenotypeSetRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetExistingValueIds(IReadOnlyCollection<SubjectGenotypeSetKey> keys)
    {
        if (keys == null || keys.Count == 0)
        {
            return new Dictionary<SubjectGenotypeSetKey, int>();
        }

        // Staged as rows with no payload, so the read runs through exactly the same join as the write path and cannot
        // disagree with it about what "the same key" means.
        var probes = keys
            .Distinct()
            .Select(key => new SubjectGenotypeSetValueToStore(key, IsUnrepresented: false, SubjectGenotypeSetData: null))
            .ToList();

        return await OverStagedChunks(probes, ReadStagedValueIds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetOrCreateValueIds(
        IReadOnlyCollection<SubjectGenotypeSetValueToStore> values)
    {
        if (values == null || values.Count == 0)
        {
            return new Dictionary<SubjectGenotypeSetKey, int>();
        }

        // The staging table's primary key is the natural key, so a repeated key would fail the bulk copy with an error
        // naming only the constraint. De-duplicating here keeps that caller mistake from becoming an opaque one - and
        // two entries for one key are, by definition, two encodings of the same genotype set.
        var distinctValues = values.DistinctBy(value => value.Key).ToList();

        return await OverStagedChunks(distinctValues, async connection =>
        {
            await connection.ExecuteAsync(InsertMissingValuesSql, commandTimeout: CommandTimeoutInSeconds);
            return await ReadStagedValueIds(connection);
        });
    }

    /// <inheritdoc />
    public async Task WriteDonorAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments)
    {
        if (assignments == null || assignments.Count == 0)
        {
            return;
        }

        var dataTable = new DataTable();
        foreach (var columnName in AssignmentColumnNames)
        {
            dataTable.Columns.Add(columnName);
        }

        foreach (var assignment in assignments)
        {
            dataTable.Rows.Add(0, assignment.DonorId, assignment.AllowedLociKey.ToString(), assignment.SubjectGenotypeSetValueId);
        }

        // No BatchSize, deliberately. Zero makes the whole write one batch, and UseInternalTransaction makes one batch
        // one transaction. With a batch size, each batch commits on its own: a failure part way through - the unique
        // index rejecting a row, a dropped connection - keeps the batches before it, and the same assignments sent
        // again then collide with those rows. At a donor batch's 8,000 rows, SQL Server can escalate to a table lock;
        // that costs nothing while a refresh is this table's only writer.
        using (var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString(), SqlBulkCopyOptions.UseInternalTransaction))
        {
            bulkCopy.BulkCopyTimeout = CommandTimeoutInSeconds;
            bulkCopy.DestinationTableName = AssignmentsTableName;
            AddColumnMappings(bulkCopy, AssignmentColumnNames);

            await bulkCopy.WriteToServerAsync(dataTable);
        }
    }

    /// <summary>
    /// Stages <paramref name="values"/> a chunk at a time and runs <paramref name="perChunk"/> over each, on one
    /// connection held for the whole operation - which is what keeps the local temp table alive between chunks.
    /// </summary>
    private async Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> OverStagedChunks(
        List<SubjectGenotypeSetValueToStore> values,
        Func<SqlConnection, Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>>> perChunk)
    {
        var ids = new Dictionary<SubjectGenotypeSetKey, int>(values.Count);

        await using var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString());
        await connection.OpenAsync();
        await connection.ExecuteAsync(CreateStagingTableSql, commandTimeout: CommandTimeoutInSeconds);

        var isFirstChunk = true;
        foreach (var chunk in values.Chunk(StagingChunkSize))
        {
            if (!isFirstChunk)
            {
                await connection.ExecuteAsync(TruncateStagingTableSql, commandTimeout: CommandTimeoutInSeconds);
            }

            isFirstChunk = false;

            await StageChunk(connection, chunk);

            // Staging sits outside the retry on purpose: a retry re-runs against the rows already staged, which is what
            // makes the attempt idempotent rather than a second insert of the same chunk.
            var chunkIds = await WithRetries(() => perChunk(connection));

            foreach (var (key, id) in chunkIds)
            {
                ids[key] = id;
            }
        }

        return ids;
    }

    /// <summary>
    /// Bulk-copies one chunk into the temp table, over the caller's own connection - a local temp table is visible only
    /// to the session that made it, so a bulk copy built from a connection string could not see it.
    /// </summary>
    /// <remarks>
    /// The column types are declared rather than left to <see cref="DataTable"/>'s default: the
    /// <c>Columns.Add("Name")</c> form used elsewhere in this project creates a STRING column, which a
    /// <c>varbinary(max)</c> payload and a <c>bit</c> cannot be written through.
    /// </remarks>
    private static async Task StageChunk(SqlConnection connection, IReadOnlyList<SubjectGenotypeSetValueToStore> chunk)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(SubjectGenotypeSetValue.HlaTypingKey), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(SubjectGenotypeSetValue.HaplotypeFrequencySetId), typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(SubjectGenotypeSetValue.AllowedLociKey), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(SubjectGenotypeSetValue.IsUnrepresented), typeof(bool)));
        dataTable.Columns.Add(new DataColumn(nameof(SubjectGenotypeSetValue.SubjectGenotypeSetData), typeof(byte[])));

        foreach (var value in chunk)
        {
            dataTable.Rows.Add(
                value.Key.HlaTypingKey,
                value.Key.HaplotypeFrequencySetId,
                value.Key.AllowedLociKey.ToString(),
                value.IsUnrepresented,
                (object) value.SubjectGenotypeSetData ?? DBNull.Value);
        }

        using (var bulkCopy = new SqlBulkCopy(connection))
        {
            bulkCopy.BulkCopyTimeout = CommandTimeoutInSeconds;
            bulkCopy.DestinationTableName = StagingTableName;
            AddColumnMappings(bulkCopy, dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName));

            await bulkCopy.WriteToServerAsync(dataTable);
        }
    }

    private static void AddColumnMappings(SqlBulkCopy bulkCopy, IEnumerable<string> columnNames)
    {
        foreach (var columnName in columnNames)
        {
            // Relies on the data table's column names matching the destination's, as everywhere else in this project.
            bulkCopy.ColumnMappings.Add(columnName, columnName);
        }
    }

    private static async Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> ReadStagedValueIds(SqlConnection connection)
    {
        var rows = await connection.QueryAsync<StoredValueIdRow>(SelectStagedValueIdsSql, commandTimeout: CommandTimeoutInSeconds);

        return rows.ToDictionary(row => row.ToKey(), row => row.Id);
    }

    private static async Task<T> WithRetries<T>(Func<Task<T>> operation)
    {
        for (var attempt = 1;; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (SqlException exception) when (attempt < MaxAttempts && RetryableErrorNumbers.Contains(exception.Number))
            {
                // Deadlocked, or lost a race to a concurrent worker. Both are resolved by running the guarded insert
                // again: the losing side's keys are simply found to exist the second time round.
            }
        }
    }

    /// <summary>
    /// <c>AllowedLociKey</c> is read as the string it is stored as, then parsed - the column holds the member name
    /// (see <c>SearchAlgorithmContext.OnModelCreating</c>), so mapping it straight onto the enum property would rest on
    /// Dapper's conversion rules for a column whose type does not match the property's.
    /// </summary>
    private sealed class StoredValueIdRow
    {
        public int Id { get; init; }
        public string HlaTypingKey { get; init; }
        public int HaplotypeFrequencySetId { get; init; }
        public string AllowedLociKey { get; init; }

        internal SubjectGenotypeSetKey ToKey() => new(HlaTypingKey, HaplotypeFrequencySetId, Enum.Parse<AllowedLociKey>(AllowedLociKey));
    }
}
