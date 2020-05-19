using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public class DonorSearchRepository : Repository, IDonorSearchRepository
    {
        private readonly ILogger logger;

        public DonorSearchRepository(IConnectionStringProvider connectionStringProvider, ILogger logger) : base(connectionStringProvider)
        {
            this.logger = logger;
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
                    filteringOptions
                ),
                GetAllDonorsForPGroupsAtLocus(
                    locus,
                    criteria.PGroupIdsToMatchInPositionTwo,
                    criteria.SearchType,
                    filteringOptions
                )
            );

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var position1Matches = results[0].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.One, locus));
            var position2Matches = results[1].Select(r => r.ToPotentialHlaMatchRelation(TypePosition.Two, locus));

            var groupedResults = position1Matches.Concat(position2Matches).GroupBy(r => r.DonorId);

            if (criteria.MismatchCount == 0)
            {
                // If no mismatch allowed at this locus, the donor must have been matched at both loci. 
                groupedResults = groupedResults.Where(g =>
                    g.Count(r => r.MatchingTypePosition == LocusPosition.One) >= 1 
                    && g.Count(r => r.MatchingTypePosition == LocusPosition.Two) >= 1
                );
            }

            logger.SendTrace($"Match Timing: Donor repo. Manipulated data for locus: {locus} in {stopwatch.ElapsedMilliseconds}ms", LogLevel.Info);
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

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 300);
            }
        }

        private async Task<IEnumerable<DonorMatch>> GetAllDonorsForPGroupsAtLocus(
            Locus locus,
            IEnumerable<int> pGroups,
            DonorType donorType,
            MatchingFilteringOptions filteringOptions
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            pGroups = pGroups.ToList();

            var filterQuery = "";

            if (filteringOptions.ShouldFilterOnDonorType)
            {
                var donorTypeClause = filteringOptions.ShouldFilterOnDonorType ? $"AND d.DonorType = {(int) donorType}" : "";

                filterQuery = $@"
INNER JOIN Donors d
ON m.DonorId = d.DonorId
{donorTypeClause}
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
                var matches = await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 300);
                logger.SendTrace($"Match Timing: Donor repo. Fetched donors at locus: {locus} in {stopwatch.ElapsedMilliseconds}ms", LogLevel.Info);
                return matches;
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

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
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