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
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public interface IDonorSearchRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria
        /// </summary>
        Task<IAsyncEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions);
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
        public async Task<IAsyncEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
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

            var pGroups = new LocusInfo<IEnumerable<int>>(criteria.PGroupIdsToMatchInPositionOne, criteria.PGroupIdsToMatchInPositionTwo);
            var fullResults = MatchAtLocus(locus, filteringOptions, criteria.SearchDonorType, pGroups, criteria.MismatchCount == 0);
            var potentialHlaMatchRelations = fullResults.SelectMany(x => x.ToPotentialHlaMatchRelations(locus));
            return potentialHlaMatchRelations;
        }

        private async IAsyncEnumerable<FullDonorMatch> MatchAtLocus(
            Locus locus,
            MatchingFilteringOptions filteringOptions,
            DonorType donorType,
            LocusInfo<IEnumerable<int>> pGroups,
            bool mustBeDoubleMatch)
        {
            if (!pGroups.Position1.Any() || !pGroups.Position2.Any())
            {
                logger.SendTrace($"No P-Groups provided at locus {locus} - SQL was not run, no donors returned.");
            }
            else
            {
                using (logger.RunTimed($"Match Timing: Donor repo. Fetched donors at locus: {locus}. For both positions."))
                {
                    // Ensure we have a Donor ID even when there is only one match at this locus.
                    const string selectDonorIdStatement = @"CASE WHEN DonorId1 IS NULL THEN DonorId2 ELSE DonorId1 END";

                    var donorTypeFilteredJoin = filteringOptions.ShouldFilterOnDonorType
                        ? $@"INNER JOIN Donors d ON {selectDonorIdStatement} = d.DonorId AND d.DonorType = {(int) donorType}"
                        : "";

                    var joinType = mustBeDoubleMatch ? "INNER" : "FULL OUTER";

                    var sql = $@"
SELECT DISTINCT {selectDonorIdStatement} as DonorId, TypePosition1, TypePosition2
FROM 
(
SELECT m.DonorId as DonorId1, TypePosition as TypePosition1
FROM {MatchingTableNameHelper.MatchingTableName(locus)} m
WHERE m.PGroup_Id IN {pGroups.Position1.ToInClause()}
) as m_1

{joinType} JOIN 
(
SELECT m.DonorId as DonorId2, TypePosition as TypePosition2
FROM {MatchingTableNameHelper.MatchingTableName(locus)} m
WHERE m.PGroup_Id IN {pGroups.Position2.ToInClause()} 
) as m_2
ON DonorId1 = DonorId2

{donorTypeFilteredJoin}
";
                    IEnumerable<FullDonorMatch> matches;

                    await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                    using (logger.RunTimed("Match Timing: Donor Repo. Phase 1 query. Per Locus. 2/2 Only."))
                    {
                        matches = await conn.QueryAsync<FullDonorMatch>(sql, commandTimeout: 1800);
                    }

                    foreach (var match in matches)
                    {
                        yield return match;
                    }
                }
            }
        }
    }
}