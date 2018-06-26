using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Data.Entity;
using Dapper;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public interface IDonorSearchRepository
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria matchRequest);
    }

    public interface IDonorInspectionRepository
    {
        Task<int> HighestDonorId();
        IBatchQueryAsync<DonorResult> AllDonors();
        Task<DonorResult> GetDonor(int donorId);
    }

    public class SqlDonorSearchRepository : IDonorSearchRepository, IDonorImportRepository, IDonorInspectionRepository
    {
        private readonly SearchAlgorithmContext context;

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        private Dictionary<string, PGroupName> pGroupDictionary;

        public SqlDonorSearchRepository(SearchAlgorithmContext context)
        {
            this.context = context;
        }

        public Task<int> HighestDonorId()
        {
            return context.Donors.OrderByDescending(d => d.DonorId).Take(1).Select(d => d.DonorId).FirstOrDefaultAsync();
        }

        public IBatchQueryAsync<DonorResult> AllDonors()
        {
            return new SqlDonorBatchQueryAsync(context.Donors);
        }

        public async Task<DonorResult> GetDonor(int donorId)
        {
            return (await context.Donors.FirstOrDefaultAsync(d => d.DonorId == donorId))?.ToDonorResult();
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

            context.Donors.AddRange(rawInputDonors.Select(d => d.ToDonorEntity()));

            await context.SaveChangesAsync();
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

            await RefreshMatchingGroupsForExistingDonor(donor);

            await context.SaveChangesAsync();
        }

        public void SetupForHlaRefresh()
        {
            // Do nothing
        }

        public async Task RefreshMatchingGroupsForExistingDonor(InputDonor donor)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Execute($@"DELETE FROM MatchingHlas WHERE DonorId = {donor.DonorId}");
            }

            await InsertPGroupMatches(donor.DonorId, donor.MatchingHla);
        }
            {
                {
                }

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
                
                var tran = conn.BeginTransaction();
                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
                {
                    sqlBulk.BatchSize = 10000;
                    sqlBulk.DestinationTableName = "PGroupNames";
                    sqlBulk.WriteToServer(dt);
                }
                tran.Commit();
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

        private async Task InsertPGroupMatches(int donorId, PhenotypeInfo<ExpandedHla> allHla)
        {
            var dataTableCreationTask = Task.Run(() =>
            {
                var dt = new DataTable();
                dt.Columns.Add("Id");
                dt.Columns.Add("DonorId");
                dt.Columns.Add("TypePosition");
                dt.Columns.Add("LocusCode");
                dt.Columns.Add("PGroup_Id");

                allHla.EachPosition((locus, position, hla) =>
                {
                    if (hla == null)
                    {
                        return;
                    }

                    foreach (var pGroup in hla.PGroups)
                    {
                        dt.Rows.Add(0, donorId, (int) position, (int) locus, FindOrCreatePGroup(pGroup).Id);
                    }
                });

                return dt;
            });

            var dataTable = await dataTableCreationTask;

            using (var sqlBulk = new SqlBulkCopy(connectionString))
            {
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "MatchingHlas";
                sqlBulk.WriteToServer(dataTable);
            }
        }

        private PGroupName FindOrCreatePGroup(string pGroupName)
        {
            if (pGroupDictionary == null)
            {
                CachePGroupDictionary();
            }

            pGroupDictionary.TryGetValue(pGroupName, out var existing);

            if (existing != null)
            {
                return existing;
            }

            return context.PGroupNames.Add(new PGroupName {Name = pGroupName});
        }

        public Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria matchRequest)
        {
            string sql = $@"
SELECT DonorId, SUM(MatchCount) AS TotalMatchCount
    FROM (
        -- get overall match count by Locus
        SELECT DonorId, Locus, MIN(MatchCount) AS MatchCount
        FROM (
            -- count number of matches in each direction
            SELECT DonorId, MatchingDirection, Locus, count(*) AS MatchCount
            FROM(
                -- get DISTINCT list of matches between search and donor type by Locus and position
                SELECT DISTINCT DonorId, MatchingDirection, Locus, TypePosition
                FROM (
                    -- Select search and donor directional match lists by Locus & matching hla name
                    -- First from type position 1 in the search hla
                    {SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.One)}
                    UNION
                    {SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.One)}
                    UNION
                    {SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.One)}
                    UNION
                    -- Next from type position 2 in the search hla
                    {SelectForLocus(Locus.A, matchRequest.LocusMismatchA, TypePositions.Two)}
                    UNION
                    {SelectForLocus(Locus.B, matchRequest.LocusMismatchB, TypePositions.Two)}
                    UNION
                    {SelectForLocus(Locus.Drb1, matchRequest.LocusMismatchDRB1, TypePositions.Two)}
                    ) AS source
                UNPIVOT (TypePosition FOR MatchingDirection IN (GvH, HvG)) AS unpivoted
                ) ByDirection
            GROUP BY DonorId, MatchingDirection, Locus
            ) ByLocus
        GROUP BY DonorId, Locus
        ) ByDonor
    GROUP BY DonorId
    HAVING SUM(MatchCount) >= {6 - matchRequest.DonorMismatchCount}
    ORDER BY TotalMatchCount DESC";

            return Task.Run(() =>
                context.Database.SqlQuery<FlatSearchQueryResult>(sql).Select(fr => fr.ToPotentialSearchResult()));
        }

        private string SelectForLocus(Locus locus, AlleleLevelLocusMatchCriteria mismatch, TypePositions typePosition)
        {
            var names = typePosition.Equals(TypePositions.One)
                ? mismatch.HlaNamesToMatchInPositionOne
                : mismatch.HlaNamesToMatchInPositionTwo;
            return $@"SELECT d.DonorId, '{locus.ToString().ToUpper()}' as Locus, d.TypePosition AS GvH, {(int) typePosition} AS HvG
                      FROM MatchingHlas d
                      JOIN dbo.PGroupNames p ON p.Id = d.PGroup_Id 
                      WHERE [Name] IN('{string.Join("', '", names)}')
                      AND d.LocusCode = {(int) locus} 
                      GROUP BY d.DonorId, d.TypePosition";
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