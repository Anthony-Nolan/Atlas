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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using MoreLinq;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public interface IDonorSearchRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria
        /// </summary>
        IAsyncEnumerable<PotentialHlaMatchRelation> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions);
    }

    public class DonorSearchRepository : Repository, IDonorSearchRepository
    {
        private const int DonorIdBatchSize = 5000;

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
        public async IAsyncEnumerable<PotentialHlaMatchRelation> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions
        )
        {
            var pGroups = new LocusInfo<IEnumerable<int>>(criteria.PGroupIdsToMatchInPositionOne, criteria.PGroupIdsToMatchInPositionTwo);
            var donorIds = filteringOptions?.DonorIds;
            if (!TypingIsRequiredAtLocus(locus) && donorIds == null)
            {
                // Donors can be untyped at these loci, which counts as a potential match.
                // This method does not return untyped donors, so cannot be used for any loci that are not guaranteed to be typed.  
                throw new ArgumentOutOfRangeException("Must provide donorIds for non-required loci.");
            }

            if (donorIds != null)
            {
                if (!TypingIsRequiredAtLocus(locus))
                {
                    // TODO: ATLAS-714: This might cause issues due to expecting donors in order? Might be ok as long as no donors are returned both from this and the typed version.
                    var untypedResults = await GetResultsForDonorsUntypedAtLocus(locus, donorIds);
                    foreach (var untypedResult in untypedResults)
                    {
                        yield return untypedResult;
                    }
                }
                
                if (donorIds.Count > DonorIdBatchSize * 5)
                {
                    logger.SendTrace($"Donor Ids available for pre-filtering, but too many for DB - filtering in C#. Locus {locus}", LogLevel.Verbose);
                    var options = new MatchingFilteringOptions
                    {
                        DonorIds = null,
                        DonorType = filteringOptions.DonorType
                    };
                    var fullResults = MatchAtLocus(locus, options, pGroups, criteria.MismatchCount == 0)
                        .Where(m => donorIds.Contains(m.DonorId));
                    // TODO: ATLAS-714: Commonise this method!
                    var convertedResults = fullResults.SelectMany(x => x.ToPotentialHlaMatchRelations(locus));
                    await foreach (var result in convertedResults)
                    {
                        yield return result;
                    }
                }
                else
                {
                    logger.SendTrace($"Pre-filtering by Donor Id. Locus {locus}", LogLevel.Verbose);
                    foreach (var donorBatch in donorIds.Batch(DonorIdBatchSize))
                    {
                        var batchOptions = new MatchingFilteringOptions
                        {
                            DonorType = filteringOptions.DonorType,
                            DonorIds = Enumerable.ToHashSet(donorBatch),
                        };
                        var batchResults = MatchAtLocus(locus, batchOptions, pGroups, criteria.MismatchCount == 0);
                        await foreach (var result in batchResults.SelectMany(x => x.ToPotentialHlaMatchRelations(locus)))
                        {
                            yield return result;
                        }
                    }
                }
            }
            else
            {
                var fullResults = MatchAtLocus(locus, filteringOptions, pGroups, criteria.MismatchCount == 0);
                var convertedResults = fullResults.SelectMany(x => x.ToPotentialHlaMatchRelations(locus));
                await foreach (var result in convertedResults)
                {
                    yield return result;
                }
            }
        }

        private async IAsyncEnumerable<DonorLocusMatch> MatchAtLocus(
            Locus locus,
            MatchingFilteringOptions filteringOptions,
            LocusInfo<IEnumerable<int>> pGroups,
            bool mustBeDoubleMatch)
        {
            if (!pGroups.Position1.Any() || !pGroups.Position2.Any())
            {
                logger.SendTrace($"No P-Groups provided at locus {locus} - SQL was not run, no donors returned.");
            }
            else if (filteringOptions.DonorIds != null && !filteringOptions.DonorIds.Any())
            {
                logger.SendTrace($"Asked to pre-filter donors at locus {locus} from an empty list - SQL was not run, no donors returned. ");
            }
            else
            {
                using (logger.RunTimed($"Match Timing: Donor repo. Matched at locus: {locus}. For both positions. Connection closed."))
                {
                    // Ensure we have a Donor ID even when there is only one match at this locus.
                    const string selectDonorIdStatement = @"CASE WHEN DonorId1 IS NULL THEN DonorId2 ELSE DonorId1 END";

                    var donorTypeFilteredJoin = filteringOptions.ShouldFilterOnDonorType
                        // ReSharper disable once PossibleInvalidOperationException - implicitly checked via ShouldFilterOnDonorType
                        ? $@"INNER JOIN Donors d ON {selectDonorIdStatement} = d.DonorId AND d.DonorType = {(int) filteringOptions.DonorType}"
                        : "";

                    var donorIdFilter = filteringOptions.ShouldFilterOnDonorIds
                        ? $"AND m.DonorId IN {filteringOptions.DonorIds.ToInClause()}"
                        : "";

                    var joinType = mustBeDoubleMatch ? "INNER" : "FULL OUTER";

                    var sql = $@"
SELECT DISTINCT {selectDonorIdStatement} as DonorId, TypePosition1, TypePosition2
FROM 
(
SELECT m.DonorId as DonorId1, TypePosition as TypePosition1
FROM {MatchingTableNameHelper.MatchingTableName(locus)} m
WHERE m.PGroup_Id IN {pGroups.Position1.ToInClause()}
{donorIdFilter}
) as m_1

{joinType} JOIN 
(
SELECT m.DonorId as DonorId2, TypePosition as TypePosition2
FROM {MatchingTableNameHelper.MatchingTableName(locus)} m
WHERE m.PGroup_Id IN {pGroups.Position2.ToInClause()} 
{donorIdFilter}
) as m_2
ON DonorId1 = DonorId2

{donorTypeFilteredJoin}

ORDER BY DonorId
";

                    // TODO: ATLAS-714: sort out SQL timing logging - currently a few places claim to be timing similar things
                    await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                    {
                        var matches = conn.Query<DonorLocusMatch>(sql, commandTimeout: 1800, buffered: false);
                        foreach (var match in matches)
                        {
                            yield return match;
                        }
                    }
                }
            }
        }
        
        private async Task<IEnumerable<PotentialHlaMatchRelation>> GetResultsForDonorsUntypedAtLocus(Locus locus, IEnumerable<int> donorIds)
        {
            using (logger.RunTimed($"Fetching untyped Donor IDs at Locus: {locus}"))
            {
                var untypedDonorIds = await GetIdsOfDonorsUntypedAtLocus(locus, donorIds);
                return untypedDonorIds.SelectMany(id => new[] {LocusPosition.One, LocusPosition.Two}.Select(
                    position =>
                        new PotentialHlaMatchRelation
                        {
                            DonorId = id,
                            Locus = locus,
                            SearchTypePosition = position,
                            MatchingTypePosition = position
                        }));
            }
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
            if (!donorIds.Any())
            {
                return new List<int>();
            }

            var sql = $@"
                SELECT DonorId FROM Donors d 
                WHERE d.DonorId IN {donorIds.ToInClause()} 
                AND {DonorHlaColumnAtLocus(locus, TypePosition.One)} IS NULL 
                AND {DonorHlaColumnAtLocus(locus, TypePosition.Two)} IS NULL
                ORDER BY DonorId
                ";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql, commandTimeout: 1800);
            }
        }
        
        private static string DonorHlaColumnAtLocus(Locus locus, TypePosition position)
        {
            var positionString = position == TypePosition.One ? "1" : "2";
            return $"{locus.ToString().ToUpper()}_{positionString}";
        }
    }
}