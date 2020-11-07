using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IHlaImportRepository
    {
        Task ImportHla(LociInfo<IList<HlaNamePGroupRelation>> hlaNamesToImport);
    }

    public class HlaImportRepository : Repository, IHlaImportRepository
    {
        public HlaImportRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task ImportHla(LociInfo<IList<HlaNamePGroupRelation>> hlaNamesToImport)
        {
            await hlaNamesToImport.WhenAllLoci(async (l, v) => await ImportHlaAtLocus(l, v));
        }

        private async Task ImportHlaAtLocus(Locus locus, IList<HlaNamePGroupRelation> hla)
        {
            var existingHla = await GetExistingHlaAtLocus(locus, hla.Select(h => h.HlaName_Id).Distinct().ToList());
            var newHla = hla.Where(h => !existingHla.Contains(h.HlaName_Id)).ToList();
            await ImportProcessedHla(locus, newHla);
        }

        private async Task ImportProcessedHla(Locus locus, IList<HlaNamePGroupRelation> newHlaRelations)
        {
            if (!newHlaRelations.Any() || !LocusSettings.MatchingOnlyLoci.Contains(locus))
            {
                return;
            }
            
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.Id));
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.HlaName_Id));
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.PGroup_Id));

            foreach (var relation in newHlaRelations)
            {
                dataTable.Rows.Add(0, relation.HlaName_Id, relation.PGroup_Id);
            }

            using (var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString()))
            {
                bulkCopy.DestinationTableName = TableName(locus);
                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }

        private async Task<ISet<int>> GetExistingHlaAtLocus(Locus locus, IList<int> hlaIds)
        {
            if (!hlaIds.Any() || !LocusSettings.MatchingOnlyLoci.Contains(locus))
            {
                return new HashSet<int>();
            }

            var tableName = TableName(locus);
            var tempTableConfig = SqlTempTableFiltering.PrepareTempTableFiltering("h", nameof(HlaNamePGroupRelation.HlaName_Id), hlaIds);

            var sql = $@"
SELECT DISTINCT h.{nameof(HlaNamePGroupRelation.HlaName_Id)} FROM {tableName} h
{tempTableConfig.FilteredJoinQueryString} 
";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await tempTableConfig.BuildTempTableFactory(conn);
                return (await conn.QueryAsync<int>(sql, new {hlaIds})).ToHashSet();
            }
        }

        private static string TableName(Locus locus) => $"HlaNamePGroupRelationAt{locus.ToString().ToUpperInvariant()}";
    }
}