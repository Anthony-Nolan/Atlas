using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// Provides methods for retrieving donor data from the active matching db, without any filtering of donors.
    /// </summary>
    public interface IDonorInspectionRepository
    {
        Task<DonorInfo> GetDonor(int donorId);
        Task<Dictionary<int, DonorInfo>> GetDonors(IEnumerable<int> donorIds);

        /// <summary>
        /// Note: this method is intended for use in debugging to lookup data for a short list (tens to hundreds) of external donor codes.
        /// It is not optimized for use in donor search.
        /// Only donors available for search will be returned.
        /// </summary>
        Task<IEnumerable<Donor>> GetAvailableDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes);

        Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds);
    }

    public class DonorInspectionRepository : Repository, IDonorInspectionRepository
    {
        public DonorInspectionRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task<DonorInfo> GetDonor(int donorId)
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var donor = await conn.QuerySingleOrDefaultAsync<Donor>($"SELECT * FROM Donors WHERE DonorId = {donorId}", commandTimeout: 300);
                return donor?.ToDonorInfo();
            }
        }

        public async Task<Dictionary<int, DonorInfo>> GetDonors(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToHashSet().ToList();
            return !donorIds.Any()
                ? new Dictionary<int, DonorInfo>()
                : (await GetDonorInfos(donorIds)).ToDictionary(d => d.DonorId, d => d);
        }

        public async Task<IEnumerable<Donor>> GetAvailableDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes)
        {
            const string sql = $@"SELECT * FROM Donors WHERE 
                    {nameof(Donor.ExternalDonorCode)} IN @{nameof(externalDonorCodes)} AND
                    {nameof(Donor.IsAvailableForSearch)} = 1";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<Donor>(sql, param: new { externalDonorCodes }, commandTimeout: 300);
            }
        }

        /// <summary>
        /// Fetches all PGroups for a batch of donors from the MatchingHlaAt$Locus tables
        /// </summary>
        public async Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();
            if (!donorIds.Any())
            {
                return new List<DonorIdWithPGroupNames>();
            }

            var results = donorIds
                .Select(id => new DonorIdWithPGroupNames { DonorId = id, PGroupNames = new PhenotypeInfo<IEnumerable<string>>() })
                .ToList();
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                foreach (var locus in LocusSettings.MatchingOnlyLoci)
                {
                    var sql = $@"
SELECT m.DonorId, m.TypePosition, p.Name as PGroupName FROM {MatchingHla.TableName(locus)} m
JOIN {HlaNamePGroupRelation.TableName(locus)} relation ON relation.HlaNameId = m.HlaNameId
JOIN PGroupNames p ON relation.PGroupId = p.Id
INNER JOIN (
    SELECT '{donorIds.FirstOrDefault()}' AS Id
    UNION ALL SELECT '{string.Join("' UNION ALL SELECT '", donorIds.Skip(1))}'
)
AS DonorIds 
ON m.DonorId = DonorIds.Id
";
                    var pGroups = await conn.QueryAsync<DonorMatchWithName>(sql, commandTimeout: 300);
                    foreach (var donorGroups in pGroups.GroupBy(p => p.DonorId))
                    {
                        foreach (var pGroupGroup in donorGroups.GroupBy(p => (TypePosition)p.TypePosition))
                        {
                            var donorResult = results.Single(r => r.DonorId == donorGroups.Key);
                            donorResult.PGroupNames = donorResult.PGroupNames.SetPosition(
                                locus,
                                pGroupGroup.Key.ToLocusPosition(),
                                pGroupGroup.Select(p => p.PGroupName)
                            );
                        }
                    }
                }
            }

            return results;
        }

        private async Task<IEnumerable<DonorInfo>> GetDonorInfos(IEnumerable<int> donorIds)
        {
            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var donorIdTempTableSetUp = SqlTempTableFiltering.PrepareTempTableFiltering("d", "DonorId", donorIds);

                var sql = $@"SELECT * FROM Donors d {donorIdTempTableSetUp.FilteredJoinQueryString}";

                await donorIdTempTableSetUp.BuildTempTableFactory(conn);

                var donors = await conn.QueryAsync<Donor>(sql, commandTimeout: 1200);
                return donors.Select(d => d.ToDonorInfo());
            }
        }
    }
}