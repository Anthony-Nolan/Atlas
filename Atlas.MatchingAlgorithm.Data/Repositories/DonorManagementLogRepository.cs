using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IDonorManagementLogRepository
    {
        Task<IEnumerable<DonorManagementLog>> GetDonorManagementLogBatch(IEnumerable<int> donorIds);

        /// <summary>
        /// Upserts a batch of donor management logs, reading the existing logs for these donors first, to determine which
        /// need creating and which need updating.
        /// This is the general purpose entry point, for use in ongoing donor management, where a donor may or may not
        /// already have a log entry.
        /// </summary>
        /// <remarks>
        /// The existence read is expensive - see <see cref="CreateDonorManagementLogBatch"/> for the cheaper create-only
        /// alternative, usable when the donors are known not to have log entries yet.
        /// </remarks>
        Task CreateOrUpdateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos);

        /// <summary>
        /// Creates a batch of donor management logs, *without* first reading the existing logs for these donors.
        /// </summary>
        /// <remarks>
        /// PRECONDITION: none of the given donors may already have a log entry. There is a unique index on
        /// <see cref="DonorManagementLog.DonorId"/>, so this will throw rather than update if any of them do.
        /// Only use this where the log table is known to hold no entries for these donors - currently only the data
        /// refresh's donor import stage, which always runs against a freshly truncated log table.
        /// Use <see cref="CreateOrUpdateDonorManagementLogBatch"/> everywhere else.
        /// </remarks>
        Task CreateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos);
    }

    public class DonorManagementLogRepository : Repository, IDonorManagementLogRepository
    {
        private const string LogTableName = "DonorManagementLogs";
        private const string DonorIdColumnName = "DonorId";
        private const string SequenceNumberColumnName = "SequenceNumberOfLastUpdate";
        private const string UpdateDateTimeColumnName = "LastUpdateDateTime";

        private readonly IAtlasLogger logger;

        public DonorManagementLogRepository(IConnectionStringProvider connectionStringProvider, IAtlasLogger logger) : base(connectionStringProvider)
        {
            this.logger = logger;
        }

        public async Task<IEnumerable<DonorManagementLog>> GetDonorManagementLogBatch(IEnumerable<int> donorIds)
        {
            var sql = $@"
                SELECT * FROM {LogTableName}
                WHERE {DonorIdColumnName} IN ({string.Join(",", donorIds)})
                ";

            // The IN clause is built from raw literals, so on a data refresh batch this is ~10,000 of them - a couple
            // of hundred KB of unique, non-parameterised SQL text per call, ~4,400 times. Sizing that is what prices
            // the parse cost and the plan-cache pollution it causes.
            logger.SendMetric(
                DataRefreshMetrics.CountMetric,
                sql.Length,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_ManagementLogSqlTextLength));

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorManagementLog>(sql, commandTimeout: 300);
            }
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos)
        {
            var infos = donorManagementInfos.ToList();

            if (!infos.Any())
            {
                return;
            }

            // Split into its read and write halves. On a data refresh, stage 20 TRUNCATEs this table, so the read
            // below can only ever return an empty set - every donor resolves to "create". Timing the two separately
            // is what turns "the read is provably useless" into a number of minutes that can be weighed against the
            // cost of removing it. Note that ongoing (non-refresh) donor management shares this path, where the read
            // is NOT useless - the refresh-window scoping of the queries is what keeps the two apart.
            List<int> donorIdsWithLogs;
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorManagementLogRead)))
            {
                donorIdsWithLogs = (await GetDonorIdsWithExistingLogs(infos.Select(i => i.DonorId))).ToList();
            }

            var (logsToUpdate, logsToCreate) = infos.ReifyAndSplit(i => donorIdsWithLogs.Contains(i.DonorId));

            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorManagementLogInsert)))
            using (var transactionScope = new AsyncTransactionScope())
            {
                await UpdateLogBatch(logsToUpdate);
                await CreateLogBatch(logsToCreate);
                transactionScope.Complete();
            }
        }

        /// <inheritdoc />
        // No transaction scope here, unlike the create-or-update path: there is only one operation to perform, and the
        // bulk copy manages its own transaction.
        public async Task CreateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos)
        {
            await CreateLogBatch(donorManagementInfos);
        }

        private async Task<IEnumerable<int>> GetDonorIdsWithExistingLogs(IEnumerable<int> donorIdsToCheck)
        {
            var existingLogs = await GetDonorManagementLogBatch(donorIdsToCheck);

            return existingLogs.Select(l => l.DonorId);
        }

        private async Task UpdateLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos)
        {
            var infos = donorManagementInfos.ToList();

            if (!infos.Any())
            {
                return;
            }

            // This UNION ALL based strategy seems sufficiently performant when bulk updating 100s of rows
            // If row count increases to the 1000s, it may be better to use a temp table instead
            var infosSelectStatement = BuildUnionAllSelectStatement(infos);
            var sql = $@"
                    UPDATE {LogTableName} 
                    SET 
                        {SequenceNumberColumnName} = infos.{SequenceNumberColumnName},
                        {UpdateDateTimeColumnName} = infos.{UpdateDateTimeColumnName}
                    FROM {LogTableName} AS logs
                    JOIN ({infosSelectStatement}) AS infos
                    ON logs.{DonorIdColumnName} = infos.{DonorIdColumnName}
                    ";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await conn.ExecuteAsync(sql, commandTimeout: 600);
            }
        }

        private static string BuildUnionAllSelectStatement(List<DonorManagementInfo> donorManagementInfos)
        {
            if (!donorManagementInfos.Any())
            {
                return string.Empty;
            }

            return Environment.NewLine + donorManagementInfos
                .Select(GetDonorManagementInfoSelectStatement)
                .StringJoin(Environment.NewLine + " UNION ALL " + Environment.NewLine);
        }

        private static string GetDonorManagementInfoSelectStatement(DonorManagementInfo info)
        {
            return "SELECT " +
                    $"{info.DonorId} AS {DonorIdColumnName}, " +
                    $"{info.UpdateSequenceNumber} AS {SequenceNumberColumnName}, " +
                    $"'{info.UpdateDateTime.ToString("O")}' AS {UpdateDateTimeColumnName}"; //Formatter needed to avoid culture date format bugs.
        }

        private async Task CreateLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos)
        {
            var infos = donorManagementInfos.ToList();

            if (!infos.Any())
            {
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add(DonorIdColumnName);
            dt.Columns.Add(SequenceNumberColumnName);
            dt.Columns.Add(UpdateDateTimeColumnName);

            foreach (var info in infos)
            {
                dt.Rows.Add(0,
                    info.DonorId,
                    info.UpdateSequenceNumber,
                    info.UpdateDateTime
                    );
            }

            using (var sqlBulk = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString(), SqlBulkCopyOptions.UseInternalTransaction))
            {
                sqlBulk.BulkCopyTimeout = 600;
                sqlBulk.BatchSize = 1000;
                sqlBulk.DestinationTableName = LogTableName;
                await sqlBulk.WriteToServerAsync(dt);
            }
        }
    }
}
