using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IHlaImportRepository
    {
        // TODO: ATLAS-749: document that this inserts HlaNames and *then* performs p group processing per hla name 
        Task<IDictionary<string, int>> ImportHla(IList<DonorInfoWithExpandedHla> hlaNamesToImport);
    }

    public class HlaImportRepository : Repository, IHlaImportRepository
    {
        private readonly IHlaNamesRepository hlaNamesRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaImportRepository(
            IHlaNamesRepository hlaNamesRepository,
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
            // TODO: ATLAS-749: Consider combining name insert + HLA import?
            this.hlaNamesRepository = hlaNamesRepository;
            this.pGroupRepository = pGroupRepository;
        }

        public async Task<IDictionary<string, int>> ImportHla(IList<DonorInfoWithExpandedHla> hlaNamesToImport)
        {
            var pGroupLookup = await pGroupRepository.EnsureAllPGroupsExist(hlaNamesToImport.AllPGroupNames());

            var hlaLookup = await hlaNamesRepository.EnsureAllHlaNamesExist(hlaNamesToImport.AllHlaNames());

            var hlaToInsert = hlaNamesToImport.Select(donor => new PhenotypeInfo<IList<HlaNamePGroupRelation>>((locus, position) =>
                    donor?.MatchingHla?.GetPosition(locus, position)?.MatchingPGroups
                        .Select(pGroup =>
                        {
                            var hlaName = donor.HlaNames.GetPosition(locus, position);
                            return hlaName == null
                                ? null
                                : new HlaNamePGroupRelation
                                {
                                    HlaNameId = hlaLookup[hlaName],
                                    PGroupId = pGroupLookup[pGroup]
                                };
                        })
                        .Where(relation => relation != null)
                        .ToList()
                )
            );

            var flattenedHlaToInsert = new LociInfo<IList<HlaNamePGroupRelation>>(
                l =>
                {
                    return hlaToInsert.SelectMany(h =>
                        {
                            var position1Relations = h.GetPosition(l, LocusPosition.One) ?? new List<HlaNamePGroupRelation>();
                            var position2Relations = h.GetPosition(l, LocusPosition.Two) ?? new List<HlaNamePGroupRelation>();
                            return position1Relations.Concat(position2Relations);
                        })
                        .Where(x => x != null)
                        .ToList();
                });

            await ImportHla(flattenedHlaToInsert);

            return hlaLookup;
        }

        private async Task ImportHla(LociInfo<IList<HlaNamePGroupRelation>> hlaNamesToImport)
        {
            await hlaNamesToImport.WhenAllLoci(async (l, v) => await ImportHlaAtLocus(l, v));
        }

        private async Task ImportHlaAtLocus(Locus locus, IList<HlaNamePGroupRelation> hla)
        {
            var existingHla = await GetExistingHlaAtLocus(locus, hla.Select(h => h.HlaNameId).Distinct().ToList());
            var newHla = hla.Where(h => !existingHla.Contains(h.HlaNameId)).ToList();
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
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.HlaNameId));
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.PGroupId));

            foreach (var relation in newHlaRelations)
            {
                dataTable.Rows.Add(0, relation.HlaNameId, relation.PGroupId);
            }

            using (var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString()))
            {
                bulkCopy.DestinationTableName = HlaNamePGroupRelation.TableName(locus);
                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }

        private async Task<ISet<int>> GetExistingHlaAtLocus(Locus locus, IList<int> hlaIds)
        {
            if (!hlaIds.Any() || !LocusSettings.MatchingOnlyLoci.Contains(locus))
            {
                return new HashSet<int>();
            }

            var tableName = HlaNamePGroupRelation.TableName(locus);
            var tempTableConfig = SqlTempTableFiltering.PrepareTempTableFiltering("h", nameof(HlaNamePGroupRelation.HlaNameId), hlaIds);

            var sql = $@"
SELECT DISTINCT h.{nameof(HlaNamePGroupRelation.HlaNameId)} FROM {tableName} h
{tempTableConfig.FilteredJoinQueryString} 
";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await tempTableConfig.BuildTempTableFactory(conn);
                return (await conn.QueryAsync<int>(sql, new {hlaIds})).ToHashSet();
            }
        }
    }
}