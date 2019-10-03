using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Config;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval
{
    public class DonorSearchRepository : Repository, IDonorSearchRepository
    {
        public DonorSearchRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        /// <summary>
        /// Fetches all donors with at least one matching p-group at the provided locus.
        /// If the matching criteria allow no mismatches, ensures a match is present for each position.
        /// This method should only be called for loci that are guaranteed to be typed - i.e. A/B/DRB1 - as it does not check for untyped donors. 
        /// </summary>
        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions
        )
        {
            if (!LocusSettings.LociPossibleToMatchInMatchingPhaseOne.Contains(locus))
            {
                // Donors can be untyped at these loci, which counts as a potential match.
                // This method does not return untyped donors, so cannot be used for any loci that are not guaranteed to be typed.  
                throw new ArgumentOutOfRangeException();
            }
            
            var results = await Task.WhenAll(
                GetAllDonorsForPGroupsAtLocus(
                    locus,
                    criteria.PGroupIdsToMatchInPositionOne,
                    criteria.SearchType,
                    criteria.Registries,
                    filteringOptions
                ),
                GetAllDonorsForPGroupsAtLocus(
                    locus,
                    criteria.PGroupIdsToMatchInPositionTwo,
                    criteria.SearchType,
                    criteria.Registries,
                    filteringOptions
                )
            );

            var position1Matches = results[0].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.One, locus));
            var position2Matches = results[1].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.Two, locus));

            var groupedResults = position1Matches.Concat(position2Matches).GroupBy(r => r.DonorId);

            if (criteria.MismatchCount == 0)
            {
                // If no mismatch allowed at this locus, the donor must have been matched at both loci. 
                groupedResults = groupedResults.Where(g =>
                    g.Count(r => r.MatchingTypePosition == TypePosition.One) >= 1 && g.Count(r => r.MatchingTypePosition == TypePosition.Two) >= 1
                );
            }

            return groupedResults.SelectMany(g => g);
        }

        public async Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocusFromDonorSelection(
            Locus locus,
            LocusSearchCriteria criteria,
            IEnumerable<int> donorIds
        )
        {
            donorIds = donorIds.ToList();

            var matchingPGroupResults = await Task.WhenAll(
                GetDonorsForPGroupsAtLocusFromDonorSelection(locus, criteria.PGroupIdsToMatchInPositionOne, donorIds),
                GetDonorsForPGroupsAtLocusFromDonorSelection(locus, criteria.PGroupIdsToMatchInPositionTwo, donorIds)
            );

            var untypedDonorIds = await GetUntypedDonorsAtLocus(locus, donorIds);
            var untypedDonorResults = untypedDonorIds.SelectMany(id => new[] {TypePosition.One, TypePosition.Two}.Select(position =>
                new PotentialHlaMatchRelation
                {
                    DonorId = id,
                    Locus = locus,
                    SearchTypePosition = position,
                    MatchingTypePosition = position
                }));

            return matchingPGroupResults[0].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.One, locus))
                .Concat(matchingPGroupResults[1].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.Two, locus)))
                .Concat(untypedDonorResults);
        }

        private async Task<IEnumerable<int>> GetUntypedDonorsAtLocus(Locus locus, IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();

            var sql = $@"
SELECT DonorId FROM Donors 
INNER JOIN (
    SELECT {donorIds.FirstOrDefault()} AS Id
    {(donorIds.Count() > 1 ? "UNION ALL SELECT" : "")}  {string.Join(" UNION ALL SELECT ", donorIds.Skip(1))}
)
AS DonorIds 
ON DonorId = DonorIds.Id 
WHERE {DonorHlaColumnAtLocus(locus, TypePosition.One)} IS NULL
AND {DonorHlaColumnAtLocus(locus, TypePosition.Two)} IS NULL
";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql, commandTimeout: 300);
            }
        }

        private async Task<IEnumerable<DonorMatch>> GetDonorsForPGroupsAtLocusFromDonorSelection(
            Locus locus,
            IEnumerable<int> pGroups,
            IEnumerable<int> donorIds
        )
        {
            donorIds = donorIds.ToList();
            pGroups = pGroups.ToList();

            var sql = $@"
SELECT InnerDonorId as DonorId, TypePosition FROM {MatchingTableNameHelper.MatchingTableName(locus)} m

RIGHT JOIN (
    SELECT {donorIds.FirstOrDefault()} AS InnerDonorId
    {(donorIds.Count() > 1 ? "UNION ALL SELECT" : "")}  {string.Join(" UNION ALL SELECT ", donorIds.Skip(1))}
)
AS InnerDonors 
ON m.DonorId = InnerDonors.InnerDonorId

INNER JOIN (
    SELECT {pGroups.FirstOrDefault()} AS PGroupId
    {(pGroups.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", pGroups.Skip(1))}
)
AS PGroupIds 
ON (m.PGroup_Id = PGroupIds.PGroupId)

GROUP BY InnerDonorId, TypePosition";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 300);
            }
        }

        private async Task<IEnumerable<DonorMatch>> GetAllDonorsForPGroupsAtLocus(
            Locus locus,
            IEnumerable<int> pGroups,
            DonorType donorType,
            IEnumerable<RegistryCode> registryCodes,
            MatchingFilteringOptions filteringOptions
        )
        {
            pGroups = pGroups.ToList();

            var filterQuery = "";

            if (filteringOptions.ShouldFilterOnDonorType || filteringOptions.ShouldFilterOnRegistry)
            {
                var donorTypeClause = filteringOptions.ShouldFilterOnDonorType ? $"AND d.DonorType = {(int) donorType}" : "";
                var registryClause = filteringOptions.ShouldFilterOnRegistry
                    ? $"AND d.RegistryCode IN ({string.Join(",", registryCodes.Select(id => (int) id))})"
                    : "";

                filterQuery = $@"
INNER JOIN Donors d
ON m.DonorId = d.DonorId
{donorTypeClause}
{registryClause}
";
            }


            var sql = $@"
SELECT m.DonorId, TypePosition FROM {MatchingTableNameHelper.MatchingTableName(locus)} m

{filterQuery}

INNER JOIN (
    SELECT {pGroups.FirstOrDefault()} AS PGroupId
    {(pGroups.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", pGroups.Skip(1))}
)
AS PGroupIds 
ON (m.PGroup_Id = PGroupIds.PGroupId)

GROUP BY m.DonorId, TypePosition";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 300);
            }
        }

        private static string DonorHlaColumnAtLocus(Locus locus, TypePosition positions)
        {
            var positionString = positions == TypePosition.One ? "1" : "2";
            return $"{locus.ToString().ToUpper()}_{positionString}";
        }
    }
}