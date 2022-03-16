using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using LoggingStopwatch;
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

        public HlaNamesRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<IDictionary<string, int>> EnsureAllHlaNamesExist(IList<string> allHlaNames, LongStopwatchCollection timerCollection)
        {
            EnsureHlaNameDictionaryCacheIsPopulated();

            var dictionaryCheckTimer = timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.NewHlaNameInsertion_FindNew_TimerKey);
            // Note that it turns out that it's quicker to run this WITHOUT a .Distinct() in it.
            var newHlaNames = allHlaNames.Where(hlaName => hlaName != null && !hlaNameToIdDictionary.ContainsKey(hlaName)).ToList();
            dictionaryCheckTimer?.Dispose();

            if (newHlaNames.Any())
            {
                await InsertHlaNames(newHlaNames); //This method refreshes the Cache after adding.
            }

            return hlaNameToIdDictionary;
        }

        private async Task InsertHlaNames(IList<string> hlaNames)
        {
            EnsureHlaNameDictionaryCacheIsPopulated();

            var newHlaNames = hlaNames.Distinct().Except(hlaNameToIdDictionary.Keys).ToList();

            if (!newHlaNames.Any())
            {
                return;
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
        }
    }
}