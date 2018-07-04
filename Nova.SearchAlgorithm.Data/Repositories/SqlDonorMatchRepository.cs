using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class SqlDonorSearchRepository : IDonorSearchRepository, IDonorImportRepository, IDonorInspectionRepository
    {
        private readonly SearchAlgorithmContext context;

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        private Dictionary<string, PGroupName> pGroupDictionary;

        public SqlDonorSearchRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var sql1 = GetAllDonorsForPGroupsAtLocusQuery(locus, criteria.HlaNamesToMatchInPositionOne);
                var sql2 = GetAllDonorsForPGroupsAtLocusQuery(locus, criteria.HlaNamesToMatchInPositionTwo);
                
                var flatResults1 = await conn.QueryAsync<DonorMatch>(sql1);
                var flatResults2 = await conn.QueryAsync<DonorMatch>(sql2);

                return flatResults1.Select(r => r.ToPotentialHlaMatchRelation(TypePositions.One, locus))
                    .Concat(flatResults2.Select(r => r.ToPotentialHlaMatchRelation(TypePositions.Two, locus)));
            }
        }

        private string GetAllDonorsForPGroupsAtLocusQuery(Locus locus, IEnumerable<string> pGroups)
        {
            return $@"
SELECT DonorId, TypePosition FROM {MatchingTableName(locus)} m
JOIN PGroupNames p 
ON m.PGroup_Id = p.Id
INNER JOIN (
    SELECT '{pGroups.FirstOrDefault()}' AS val
    UNION ALL SELECT '{string.Join("' UNION ALL SELECT '", pGroups.Skip(1))}'
)
AS temp 
ON p.Name = temp.val
GROUP BY DonorId, TypePosition";
        }

        public Task<int> HighestDonorId()
        {
            return context.Donors.OrderByDescending(d => d.DonorId).Take(1).Select(d => d.DonorId).FirstOrDefaultAsync();
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var donors = conn.Query<Donor>("SELECT * FROM donors");
                return new SqlDonorBatchQueryAsync(donors);
            }
        }

        public async Task<DonorResult> GetDonor(int donorId)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var donor = await conn.QuerySingleOrDefaultAsync<Donor>($"SELECT * FROM Donors WHERE DonorId = {donorId}");
                return donor?.ToDonorResult();
            }
        }

        public async Task InsertDonor(RawInputDonor donor)
        {
            context.Donors.Add(donor.ToDonorEntity());
            await context.SaveChangesAsync();
        }

        public async Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors)
        {
            var rawInputDonors = donors.ToList();

            if (!rawInputDonors.Any())
            {
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("DonorId");
            dt.Columns.Add("DonorType");
            dt.Columns.Add("RegistryCode");
            dt.Columns.Add("A_1");
            dt.Columns.Add("A_2");
            dt.Columns.Add("B_1");
            dt.Columns.Add("B_2");
            dt.Columns.Add("C_1");
            dt.Columns.Add("C_2");
            dt.Columns.Add("DPB1_1");
            dt.Columns.Add("DPB1_2");
            dt.Columns.Add("DQB1_1");
            dt.Columns.Add("DQB1_2");
            dt.Columns.Add("DRB1_1");
            dt.Columns.Add("DRB1_2");

            foreach (var donor in rawInputDonors)
            {
                dt.Rows.Add(0,
                    donor.DonorId,
                    (int) donor.DonorType,
                    (int) donor.RegistryCode,
                    donor.HlaNames.A_1, donor.HlaNames.A_2, 
                    donor.HlaNames.B_1, donor.HlaNames.B_2, 
                    donor.HlaNames.C_1, donor.HlaNames.C_2,
                    donor.HlaNames.DPB1_1, donor.HlaNames.DPB1_2, 
                    donor.HlaNames.DQB1_1, donor.HlaNames.DQB1_2,
                    donor.HlaNames.DRB1_1, donor.HlaNames.DRB1_2);
            }
            
            using (var sqlBulk = new SqlBulkCopy(connectionString))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "Donors";
                sqlBulk.WriteToServer(dt);
            }
        }

        public async Task AddOrUpdateDonorWithHla(InputDonor donor)
        {
            var result = await context.Donors.FirstOrDefaultAsync(d => d.DonorId == donor.DonorId);
            if (result == null)
            {
                context.Donors.Add(donor.ToDonorEntity());
            }
            else
            {
                result.CopyRawHlaFrom(donor);
            }

            await RefreshMatchingGroupsForExistingDonorBatch(new List<InputDonor>{ donor });

            await context.SaveChangesAsync();
        }

        public async Task RefreshMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonor> inputDonors)
        {
            var loci = Enum.GetValues(typeof(Locus)).Cast<Locus>();
            await Task.WhenAll(loci.Select(l => RefreshMatchingGroupsForExistingDonorBatchAtLocus(inputDonors, l)));
        }

        public void SetupForHlaRefresh()
        {
            // Do nothing
        }

        private async Task RefreshMatchingGroupsForExistingDonorBatchAtLocus(IEnumerable<InputDonor> donors, Locus locus)
        {
            if (locus == Locus.Dpb1)
            {
                return;
            }
            
            var tableName = MatchingTableName(locus);
            
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                conn.Execute($@"DELETE FROM {tableName} WHERE DonorId IN('{string.Join("', '", donors.Select(d => d.DonorId))}')", null, transaction);

                var dataTableGenerationTask = Task.Run(() =>
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Id");
                    dt.Columns.Add("DonorId");
                    dt.Columns.Add("TypePosition");
                    dt.Columns.Add("PGroup_Id");

                    foreach (var donor in donors)
                    {
                        donor.MatchingHla.EachPosition((l, p, h) =>
                        {
                            if (h == null || l != locus)
                            {
                                return;
                            }

                            foreach (var pGroup in h.PGroups)
                            {
                                dt.Rows.Add(0, donor.DonorId, (int) p, FindOrCreatePGroup(pGroup));
                            }
                        });
                    }

                    return dt;
                });

                var dataTable = await dataTableGenerationTask;

                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                {
                    sqlBulk.BatchSize = 10000;
                    sqlBulk.DestinationTableName = tableName;
                    sqlBulk.WriteToServer(dataTable);
                }
                transaction.Commit();
                conn.Close();
            }
        }

        private string MatchingTableName(Locus locus)
        {
            return "MatchingHlaAt" + locus;
        }
        
        public void InsertPGroups(IEnumerable<string> pGroups)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                var existingPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames").Select(p => p.Name);
                
                var dt = new DataTable();
                dt.Columns.Add("Id");
                dt.Columns.Add("Name");

                foreach (var pg in pGroups.Distinct().Except(existingPGroups)) 
                {
                    dt.Rows.Add(0, pg);
                }
                
                var transaction = conn.BeginTransaction();
                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, transaction))
                {
                    sqlBulk.BatchSize = 10000;
                    sqlBulk.DestinationTableName = "PGroupNames";
                    sqlBulk.WriteToServer(dt);
                }
                transaction.Commit();
                conn.Close();
            }

            CachePGroupDictionary();
        }

        private void CachePGroupDictionary()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var innerPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames");
                pGroupDictionary = innerPGroups.Distinct(new DistinctPGroupNameComparer()).ToDictionary(p => p.Name);
            }
        }

        private int FindOrCreatePGroup(string pGroupName)
        {
            if (pGroupDictionary == null)
            {
                CachePGroupDictionary();
            }

            pGroupDictionary.TryGetValue(pGroupName, out var existing);

            if (existing != null)
            {
                return existing.Id;
            }
            
            string sql = @"
INSERT INTO PGroupNames (Name) VALUES (@PGroupName);
SELECT CAST(SCOPE_IDENTITY() as int)";

            int newId;
            
            using (var conn = new SqlConnection(connectionString))
            {
                newId = conn.Query<int>(sql, new { PGroupName = pGroupName }).Single();
            }

            CachePGroupDictionary();
            return newId;
        }
    }

    class DistinctPGroupNameComparer : IEqualityComparer<PGroupName>
    {
        public bool Equals(PGroupName x, PGroupName y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(PGroupName obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}