using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
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
    /// Purely an optimisation for the caller: it lets an expensive imputation be skipped for a typing that an earlier
    /// batch of the same refresh has already paid for, including a batch from a stopped run that is now continued. Under
    /// the agreed design, rows do not outlive their refresh - ATL-231 adds both tables to the clean-up that starts each
    /// refresh - which is why the key can leave out the HLA nomenclature version. This call is NOT how duplicate rows
    /// are prevented - that is <see cref="GetOrCreateValueIds"/>'s guarded insert - so a caller may skip it entirely
    /// and still be correct, just slower.
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
    /// Points each donor at the value it resolves to, per locus combination: inserts the rows that are missing, and
    /// updates the rows that point at a different value. Rows that already point at the given value are left alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Safe to repeat.</b> Sending the same assignments again leaves the table as the first call did. That is what
    /// a continued refresh, a message Service Bus delivers twice, or a failed batch sent again all rely on. The update
    /// is what a differential import relies on: an updated donor already has its rows, and they must move to the
    /// donor's new values rather than keep the old ones.
    /// </para>
    ///
    /// <para>
    /// <b>All or nothing.</b> The upsert is one statement, so a failed call keeps no row from that call. Inside an
    /// ambient transaction it joins that transaction, and a rollback there undoes it too.
    /// </para>
    ///
    /// <para>
    /// The same (donor, combination) may appear more than once only with the same value id. Two different value ids
    /// for one pair have no right answer, so that throws <see cref="ArgumentException"/> before anything is written.
    /// </para>
    /// </remarks>
    Task UpsertDonorAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments);
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
/// <b>No transactions of its own.</b> Each write is its own autocommit statement, as everywhere else in this project.
/// The orchestrator's ordering - values first, then donor assignments - is what makes a crash between the two
/// recoverable, not a scope around them. A caller that needs the assignments in its own transaction (the differential
/// import writes them with the donor's HLA) opens an ambient scope, and the upsert joins it.
/// </para>
/// </summary>
public class SubjectGenotypeSetRepository : Repository, ISubjectGenotypeSetRepository
{
    private const string ValuesTableName = "SubjectGenotypeSetValues";
    private const string AssignmentsTableName = "DonorSubjectGenotypeSets";
    private const string StagingTableName = "#StagedGenotypeSetValues";
    private const string AssignmentStagingTableName = "#StagedDonorAssignments";

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
    /// Transient-failure attempts for one chunk's insert and read-back, and for the donor-assignment upsert when no
    /// ambient transaction is open.
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

    private const string TruncateStagingTableSql = $"TRUNCATE TABLE {StagingTableName}";

    /// <summary>
    /// The primary key is the unique index's key, so each (donor, combination) is staged once - which
    /// <c>MERGE</c> requires, since it rejects a target row matched by more than one source row.
    /// </summary>
    private const string CreateAssignmentStagingTableSql = $"""
        CREATE TABLE {AssignmentStagingTableName} (
            {nameof(DonorSubjectGenotypeSet.DonorId)}                   int          NOT NULL,
            {nameof(DonorSubjectGenotypeSet.AllowedLociKey)}            nvarchar(16) COLLATE DATABASE_DEFAULT NOT NULL,
            {nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)} int          NOT NULL,
            PRIMARY KEY (
                {nameof(DonorSubjectGenotypeSet.DonorId)},
                {nameof(DonorSubjectGenotypeSet.AllowedLociKey)}))
        """;

    /// <summary>
    /// Upserts every staged assignment in one statement.
    ///
    /// <para>
    /// <b>One statement, not an <c>UPDATE</c> then an <c>INSERT</c>.</b> An autocommit statement is its own
    /// transaction, so the whole call is kept or lost as one without a scope around it. Two statements would need
    /// one, and a failure between them would keep the update without the insert.
    /// </para>
    ///
    /// <para>
    /// <b><c>TABLOCKX</c>, not row locks.</b> Two concurrent upserts must neither both find no row (and one fail the
    /// unique index) nor deadlock. <c>HOLDLOCK</c> alone does the first but not the second: on a small table the
    /// <c>MERGE</c> reads a range of the unique index rather than seeking each key, so two upserts can take the same
    /// key-range locks in opposite order - the end-of-index key and an existing key - and deadlock. Measured with 8
    /// concurrent upserts of the same donor: with <c>HOLDLOCK</c>, 4 of 10 runs deadlocked and took 2-9 s each while
    /// SQL Server detected it, and only the retry made them pass. An exclusive table lock is one lock, taken first, so
    /// two upserts wait for each other instead.
    /// </para>
    ///
    /// <para>
    /// What this costs is that writers always wait for each other. A donor batch is at most a few thousand small rows
    /// and one writer at a time is the normal case for this table, and a full batch escalated to a table lock anyway.
    /// Inside a caller's transaction (the differential import) the lock is held until that transaction ends. Readers
    /// are not blocked under read-committed snapshot, which Azure SQL Database turns on by default.
    /// </para>
    ///
    /// <para>
    /// The update is guarded by the value id differing, so a repeated call rewrites nothing.
    /// </para>
    /// </summary>
    private const string UpsertStagedAssignmentsSql = $"""
        MERGE {AssignmentsTableName} WITH (TABLOCKX) AS t
        USING {AssignmentStagingTableName} AS s
            ON t.{nameof(DonorSubjectGenotypeSet.DonorId)} = s.{nameof(DonorSubjectGenotypeSet.DonorId)}
           AND t.{nameof(DonorSubjectGenotypeSet.AllowedLociKey)} = s.{nameof(DonorSubjectGenotypeSet.AllowedLociKey)}
        WHEN MATCHED AND t.{nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)} <> s.{nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)} THEN
            UPDATE SET t.{nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)} = s.{nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)}
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (
                {nameof(DonorSubjectGenotypeSet.DonorId)},
                {nameof(DonorSubjectGenotypeSet.AllowedLociKey)},
                {nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)})
            VALUES (
                s.{nameof(DonorSubjectGenotypeSet.DonorId)},
                s.{nameof(DonorSubjectGenotypeSet.AllowedLociKey)},
                s.{nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId)});
        """;

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
    public async Task UpsertDonorAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments)
    {
        if (assignments == null || assignments.Count == 0)
        {
            return;
        }

        var distinctAssignments = DistinctAssignments(assignments);

        // One connection for the whole call: the temp table is visible only to the session that made it. Opened inside
        // an ambient transaction, the connection enlists in it, and so do the bulk copy and the MERGE that run over it.
        await using var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString());
        await connection.OpenAsync();
        await connection.ExecuteAsync(CreateAssignmentStagingTableSql, commandTimeout: CommandTimeoutInSeconds);

        await StageAssignments(connection, distinctAssignments);

        // A retry re-runs the MERGE against the rows already staged, so it cannot apply anything twice. It is only safe
        // with no ambient transaction: a deadlock victim's transaction is rolled back, so inside a caller's scope there
        // is nothing left to retry in, and the failure must reach the caller whose work was undone.
        if (Transaction.Current == null)
        {
            await WithRetries(() => connection.ExecuteAsync(UpsertStagedAssignmentsSql, commandTimeout: CommandTimeoutInSeconds));
        }
        else
        {
            await connection.ExecuteAsync(UpsertStagedAssignmentsSql, commandTimeout: CommandTimeoutInSeconds);
        }
    }

    /// <summary>
    /// One assignment per (donor, combination). Exact repeats are dropped - they are the same instruction twice. Two
    /// different value ids for one pair are a caller bug with no right answer, so they fail here, before anything is
    /// written, rather than as an unclear primary-key error from the staging table.
    /// </summary>
    private static List<DonorSubjectGenotypeSetAssignment> DistinctAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments)
    {
        var distinct = new List<DonorSubjectGenotypeSetAssignment>(assignments.Count);

        foreach (var group in assignments.GroupBy(assignment => (assignment.DonorId, assignment.AllowedLociKey)))
        {
            var valueIds = group.Select(assignment => assignment.SubjectGenotypeSetValueId).Distinct().ToList();
            if (valueIds.Count > 1)
            {
                throw new ArgumentException(
                    $"Donor {group.Key.DonorId} is assigned more than one value at {group.Key.AllowedLociKey}: {string.Join(", ", valueIds)}.",
                    nameof(assignments));
            }

            distinct.Add(group.First());
        }

        return distinct;
    }

    /// <summary>
    /// Bulk-copies the assignments into the temp table, over the caller's own connection. No
    /// <see cref="SqlBulkCopyOptions.UseInternalTransaction"/>: the staging table needs no atomicity of its own, and the
    /// copy should simply run in whatever transaction the connection is already in.
    /// </summary>
    private static async Task StageAssignments(SqlConnection connection, IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(DonorSubjectGenotypeSet.DonorId), typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(DonorSubjectGenotypeSet.AllowedLociKey), typeof(string)));
        dataTable.Columns.Add(new DataColumn(nameof(DonorSubjectGenotypeSet.SubjectGenotypeSetValueId), typeof(int)));

        foreach (var assignment in assignments)
        {
            dataTable.Rows.Add(assignment.DonorId, assignment.AllowedLociKey.ToString(), assignment.SubjectGenotypeSetValueId);
        }

        using (var bulkCopy = new SqlBulkCopy(connection))
        {
            bulkCopy.BulkCopyTimeout = CommandTimeoutInSeconds;
            bulkCopy.DestinationTableName = AssignmentStagingTableName;
            AddColumnMappings(bulkCopy, dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName));

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
