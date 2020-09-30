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
        /// Returns donor matches at a given locus matching the search criteria.
        ///
        /// Returns objects of the type <see cref="PotentialHlaMatchRelation"/>, which is a relationship for a specific pair of PGroups for the donor/patient.
        /// e.g. stating that the patient's hla at position 1 matches the donor's at position 2.
        ///
        /// As such multiple relations can be returned per donor. Donor relations returned by this method must be grouped - i.e. once a relation is seen
        /// for a donor in the resulting enumerable, if any other relations exist for that donor, they must be returned *before* any relations for other donors.
        ///
        /// This can be achieved via `ORDER BY` statements in the SQL requests. 
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
                // This method is not expected to be performant enough without donorIds to pre-filter. 
                throw new InvalidOperationException("Must provide donorIds for non-required loci.");
            }

            if (donorIds == null)
            {
                var results = MatchAtLocus(locus, filteringOptions, pGroups, criteria.MismatchCount == 0);
                await foreach (var result in results)
                {
                    yield return result;
                }
            }
            else
            {
                if (!TypingIsRequiredAtLocus(locus))
                {
                    var untypedResults = await GetResultsForDonorsUntypedAtLocus(locus, donorIds.ToList());
                    foreach (var untypedResult in untypedResults)
                    {
                        yield return untypedResult;
                    }
                }

                // This is explicitly not an else if! We need to return untyped donors *in addition to* matching donors
                // TODO: ATLAS-714: Consider this carefully - I think this means we're performing SQL queries too many times?
                if (donorIds.Count > DonorIdBatchSize * 5)
                {
                    logger.SendTrace(
                        $"{donorIds.Count} Donor Ids available for pre-filtering, but too many for DB - filtering in C#. Locus {locus}",
                        LogLevel.Verbose
                    );
                    var options = new MatchingFilteringOptions {DonorIds = null, DonorType = filteringOptions.DonorType};

                    var results = MatchAtLocus(locus, options, pGroups, criteria.MismatchCount == 0)
                        .Where(m => donorIds.Contains(m.DonorId));

                    await foreach (var result in results)
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

                        await foreach (var result in batchResults)
                        {
                            yield return result;
                        }
                    }
                }
            }
        }

        private async IAsyncEnumerable<PotentialHlaMatchRelation> MatchAtLocus(
            Locus locus,
            MatchingFilteringOptions filteringOptions,
            LocusInfo<IEnumerable<int>> pGroups,
            bool mustBeDoubleMatch)
        {
            var sqlMatchResults = MatchAtLocusSql(locus, filteringOptions, pGroups, mustBeDoubleMatch);
            var relations = sqlMatchResults.SelectMany(x => x.ToPotentialHlaMatchRelations(locus));
            await foreach (var relation in relations)
            {
                yield return relation;
            }
        }

        /// <summary>
        /// Performs matching at the specified locus.
        ///
        /// Streams donors from the database, which are fed through to later loci in batches - so we can expect the connection for the first locus
        /// to remain open for some time. 
        /// </summary>
        /// <param name="locus">Locus to perform matching on.</param>
        /// <param name="filteringOptions">Provides information which can be used to perform pre-filtering in the SQL request. </param>
        /// <param name="pGroups">pGroups to match at each position at this locus. </param>
        /// <param name="mustBeDoubleMatch">
        /// If true, no mismatches are allowed at this locus, allowing the query to perform a logical AND on the two positions.
        /// Else, at least one mismatch is allowed here, so we must instead perform a logical OR. 
        /// </param>
        /// <returns></returns>
        private async IAsyncEnumerable<DonorLocusMatch> MatchAtLocusSql(
            Locus locus,
            MatchingFilteringOptions filteringOptions,
            LocusInfo<IEnumerable<int>> pGroups,
            bool mustBeDoubleMatch)
        {
            // Technically this would incorrectly reject empty P-group collections at a single locus only. We assert that this will never happen, as partially typed loci are disallowed,
            // and null expressing alleles (the only way to have a null p group) are handled by copying the expressing allele's P-groups to the null position.
            // If either of these facts changes, this validation may be incorrect.
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

                    await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                    {
                        // This is streamed from the database via `buffered: false` - this allows us to minimise our memory footprint, by not loading
                        // all donors into memory at once, and filtering as we go. 
                        var matches = conn.Query<DonorLocusMatch>(sql, commandTimeout: 3600, buffered: false);
                        foreach (var match in matches)
                        {
                            yield return match;
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<PotentialHlaMatchRelation>> GetResultsForDonorsUntypedAtLocus(Locus locus, IList<int> donorIds)
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

        private async Task<IEnumerable<int>> GetIdsOfDonorsUntypedAtLocus(Locus locus, IList<int> donorIds)
        {
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