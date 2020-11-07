using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using LoggingStopwatch;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IHlaNamesRepository
    {
        Task<IDictionary<string, int>> EnsureAllHlaNamesExist(List<string> allHlaNames, LongStopwatchCollection timerCollection = null);
    }

    public class HlaNamesRepository : Repository, IHlaNamesRepository
    {
        public HlaNamesRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<IDictionary<string, int>> EnsureAllHlaNamesExist(List<string> allHlaNames, LongStopwatchCollection timerCollection = null)
        {
            allHlaNames = allHlaNames.Where(hla => hla != null).Distinct().ToList();
            var existingHlaNames = await GetExistingHlaNames(allHlaNames);
            var newHlaNames = allHlaNames.Except(existingHlaNames).ToList();
            await InsertHlaNames(newHlaNames);

            return await GetHlaNameIds(allHlaNames);
        }

        private async Task<IDictionary<string, int>> GetHlaNameIds(IList<string> allHlaNames)
        {
            var tempTableFilterDetails = SqlTempTableFiltering.PrepareTempTableFiltering("h", nameof(HlaName.Name), allHlaNames);
            
            var sql = $@"
SELECT h.{nameof(HlaName.Name)}, h.{nameof(HlaName.Id)} 
FROM HlaNames h 
{tempTableFilterDetails.FilteredJoinQueryString}";
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await tempTableFilterDetails.BuildTempTableFactory(conn);
                return (await conn.QueryAsync<HlaName>(sql, new {allHlaNames})).ToDictionary(hlaName => hlaName.Name, hlaName => hlaName.Id);
            }
        }

        private async Task<IList<string>> GetExistingHlaNames(IList<string> namesToCheck)
        {
            return (await GetHlaNameIds(namesToCheck)).Keys.ToList();
        }

        private async Task InsertHlaNames(IList<string> hlaNames)
        {
            if (!hlaNames.Any())
            {
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");

            foreach (var hlaName in hlaNames)
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
        }
    }
}