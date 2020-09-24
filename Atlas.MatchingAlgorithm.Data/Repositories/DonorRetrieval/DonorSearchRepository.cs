using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public interface IDonorSearchRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria
        /// </summary>
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions
        );
    }

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
                GetAllDonorsForPGroupsAtLocus(locus, criteria.PGroupIdsToMatchInPositionOne, criteria.SearchDonorType, filteringOptions),
                GetAllDonorsForPGroupsAtLocus(locus, criteria.PGroupIdsToMatchInPositionTwo, criteria.SearchDonorType, filteringOptions)
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

            logger.SendTrace($"Match Timing: Donor repo. Manipulated data for locus: {locus} in {stopwatch.ElapsedMilliseconds}ms");
            return groupedResults.SelectMany(g => g);
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

            var donorTypeFilteredJoin = "";

            if (filteringOptions.ShouldFilterOnDonorType)
            {
                var donorTypeClause = filteringOptions.ShouldFilterOnDonorType ? $"AND d.DonorType = {(int)donorType}" : "";

                donorTypeFilteredJoin = $@"
                    INNER JOIN Donors d
                    ON m.DonorId = d.DonorId
                    {donorTypeClause}
                    ";
            }

            var sql = $@"
                SELECT m.DonorId, TypePosition FROM {MatchingTableNameHelper.MatchingTableName(locus)} m

                {donorTypeFilteredJoin}

                INNER JOIN (
                    SELECT {pGroups.FirstOrDefault()} AS PGroupId
                    {(pGroups.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", pGroups.Skip(1))}
                )
                AS PGroupIds 
                ON (m.PGroup_Id = PGroupIds.PGroupId)

                GROUP BY m.DonorId, TypePosition";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var matches = await conn.QueryAsync<DonorMatch>(sql, commandTimeout: 1800);
                logger.SendTrace($"Match Timing: Donor repo. Fetched donors at locus: {locus} in {stopwatch.ElapsedMilliseconds}ms");
                return matches;
            }
        }
    }
}