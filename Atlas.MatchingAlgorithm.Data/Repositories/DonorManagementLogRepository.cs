﻿using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IDonorManagementLogRepository
    {
        Task<IEnumerable<DonorManagementLog>> GetDonorManagementLogBatch(IEnumerable<int> donorIds);
        Task CreateOrUpdateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos);
    }

    public class DonorManagementLogRepository : Repository, IDonorManagementLogRepository
    {
        private const string LogTableName = "DonorManagementLogs";
        private const string DonorIdColumnName = "DonorId";
        private const string SequenceNumberColumnName = "SequenceNumberOfLastUpdate";
        private const string UpdateDateTimeColumnName = "LastUpdateDateTime";

        public DonorManagementLogRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<IEnumerable<DonorManagementLog>> GetDonorManagementLogBatch(IEnumerable<int> donorIds)
        {
            var sql = $@"
                SELECT * FROM {LogTableName}
                WHERE {DonorIdColumnName} IN ({string.Join(",", donorIds)})
                ";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorManagementLog>(sql, commandTimeout: 300);
            }
        }

        public async Task CreateOrUpdateDonorManagementLogBatch(IEnumerable<DonorManagementInfo> donorManagementInfos)
        {
            var infos = donorManagementInfos.ToList();

            if (!infos.Any())
            {
                return;
            }

            var donorIdsWithLogs = (await GetDonorIdsWithExistingLogs(infos.Select(i => i.DonorId))).ToList();

            var (logsToUpdate, logsToCreate) = infos.ReifyAndSplit(i => donorIdsWithLogs.Contains(i.DonorId));

            using (var transactionScope = new AsyncTransactionScope())
            {
                await UpdateLogBatch(logsToUpdate);
                await CreateLogBatch(logsToCreate);
                transactionScope.Complete();
            }
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
