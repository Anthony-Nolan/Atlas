using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public class PreFilteredDonorSearchRepository : Repository, IPreFilteredDonorSearchRepository
    {
        public PreFilteredDonorSearchRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
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

            var untypedDonorResults = await GetResultsForDonorsUntypedAtLocus(locus, donorIds);

            return matchingPGroupResults[0].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.One, locus))
                .Concat(matchingPGroupResults[1].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.Two, locus)))
                .Concat(untypedDonorResults);
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

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 300);
            }
        }

        private async Task<IEnumerable<PotentialHlaMatchRelation>> GetResultsForDonorsUntypedAtLocus(Locus locus, IEnumerable<int> donorIds)
        {
            if (TypingIsRequiredAtLocus(locus))
            {
                return new List<PotentialHlaMatchRelation>();
            }

            var untypedDonorIds = await GetIdsOfDonorsUntypedAtLocus(locus, donorIds);
            var untypedDonorResults = untypedDonorIds.SelectMany(id => new[] { LocusPosition.One, LocusPosition.Two }.Select(
                position =>
                    new PotentialHlaMatchRelation
                    {
                        DonorId = id,
                        Locus = locus,
                        SearchTypePosition = position,
                        MatchingTypePosition = position
                    }));
            return untypedDonorResults;
        }

        private static bool TypingIsRequiredAtLocus(Locus locus)
        {
            return TypingIsRequiredInDatabaseAtLocusPosition(locus, TypePosition.One) &&
                   TypingIsRequiredInDatabaseAtLocusPosition(locus, TypePosition.Two);
        }

        private static bool TypingIsRequiredInDatabaseAtLocusPosition(Locus locus, TypePosition position)
        {
            var locusColumnName = DonorHlaColumnAtLocus(locus, position);
            var locusProperty = typeof(Donor).GetProperty(locusColumnName);

            return Attribute.IsDefined(locusProperty, typeof(RequiredAttribute));
        }

        private async Task<IEnumerable<int>> GetIdsOfDonorsUntypedAtLocus(Locus locus, IEnumerable<int> donorIds)
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

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql, commandTimeout: 300);
            }
        }

        private static string DonorHlaColumnAtLocus(Locus locus, TypePosition position)
        {
            var positionString = position == TypePosition.One ? "1" : "2";
            return $"{locus.ToString().ToUpper()}_{positionString}";
        }
    }
}