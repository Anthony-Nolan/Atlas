using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IHlaNamesRepository
    {
        /// <summary>
        /// For all HLA names provided - adds to the data store if they are not already present.
        /// </summary>
        /// <returns>A dictionary with Key = HlaName, Value = Database ID</returns>
        Task<IDictionary<string, int>> EnsureAllHlaNamesExist(IList<string> allHlaNames, LongStopwatchCollection timerCollection = null);
    }

    public class HlaNamesRepository : Repository, IHlaNamesRepository
    {
        private IDictionary<string, int> hlaNameToIdDictionary;

        private readonly IAtlasLogger logger;

        public HlaNamesRepository(IConnectionStringProvider connectionStringProvider, IAtlasLogger logger) : base(connectionStringProvider)
        {
            this.logger = logger;
        }

        public async Task<IDictionary<string, int>> EnsureAllHlaNamesExist(IList<string> allHlaNames, LongStopwatchCollection timerCollection)
        {
            EnsureHlaNameDictionaryCacheIsPopulated();

            var dictionaryCheckTimer = timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.NewHlaNameInsertion_FindNew_TimerKey);
            // Note that it turns out that it's quicker to run this WITHOUT a .Distinct() in it.
            var newHlaNames = allHlaNames.Where(hlaName => hlaName != null && !hlaNameToIdDictionary.ContainsKey(hlaName)).ToList();
            dictionaryCheckTimer?.Dispose();

            var insertedCount = 0;
            if (newHlaNames.Any())
            {
                insertedCount = await InsertHlaNames(newHlaNames); //This method refreshes the Cache after adding.
            }

            // Emitted for EVERY batch, including the (expected) zeros - the zeros ARE the finding. The nomenclature has
            // a fixed name set that tens of millions of donors re-use, so if novelty dies out early then everything the
            // ImportHla path does per batch thereafter is pure waste. A counter only emitted on the insert path could
            // never show that, because it would simply stop being emitted.
            logger.SendMetric(
                DataRefreshMetrics.CountMetric,
                insertedCount,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_NewHlaNamesPerBatch));

            return hlaNameToIdDictionary;
        }

        /// <returns>The number of names actually inserted (i.e. after de-duplication against the cache).</returns>
        private async Task<int> InsertHlaNames(IList<string> hlaNames)
        {
            EnsureHlaNameDictionaryCacheIsPopulated();

            var newHlaNames = hlaNames.Distinct().Except(hlaNameToIdDictionary.Keys).ToList();

            if (!newHlaNames.Any())
            {
                return 0;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");

            foreach (var hlaName in newHlaNames)
            {
                dt.Rows.Add(0, hlaName);
            }

            using (var sqlBulk = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString()))
            {
                sqlBulk.BulkCopyTimeout = 600;
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "HlaNames";
                await sqlBulk.WriteToServerAsync(dt);
            }

            // We need to get the new Ids back out.
            ForceCacheHlaNameDictionary();

            return newHlaNames.Count;
        }

        private void EnsureHlaNameDictionaryCacheIsPopulated()
        {
            if (hlaNameToIdDictionary == null)
            {
                ForceCacheHlaNameDictionary();
            }
        }

        private void ForceCacheHlaNameDictionary()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var innerHlaNames = conn.Query<HlaName>(
                    $"SELECT h.{nameof(HlaName.Name)}, h.{nameof(HlaName.Id)} FROM HlaNames h ",
                    commandTimeout: 300);
                hlaNameToIdDictionary = innerHlaNames
                    .DistinctBy(hla => hla.Name)
                    .ToDictionary(h => h.Name, h => h.Id);
            }

            // Sizes the table that this method re-reads IN FULL every time a new name appears. Pairing the row count
            // with the EnsureHlaNamesExist duration tests the "cost grows ~quadratically with table size" claim
            // directly, rather than inferring it from a rising duration alone.
            logger.SendMetric(
                DataRefreshMetrics.CountMetric,
                hlaNameToIdDictionary.Count,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaNamesTableRows));
        }
    }
}
