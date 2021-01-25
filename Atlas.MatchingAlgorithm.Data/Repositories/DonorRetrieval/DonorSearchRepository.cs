using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
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
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

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
            MatchingFilteringOptions filteringOptions,
            DateTime? cutOffDate);
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
        public async IAsyncEnumerable<PotentialHlaMatchRelation> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions,
            DateTime? cutOffDate
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

            var preFilteringLogMessage = donorIds == null ? "" : $"Pre-filtering by donor id - Batch of {donorIds.Count} donors.";
            var logMessage = $"DonorSearchRepository: Matching at Locus {locus}. {preFilteringLogMessage}";
            logger.SendTrace(logMessage, LogLevel.Verbose);

            if (donorIds != null && !TypingIsRequiredAtLocus(locus))
            {
                var untypedResults = await GetResultsForDonorsUntypedAtLocus(locus, donorIds.ToList());
                foreach (var untypedResult in untypedResults)
                {
                    yield return untypedResult;
                }
            }

            var typedResults = MatchAtLocus(locus, filteringOptions, pGroups, criteria.MismatchCount == 0, cutOffDate);

            await foreach (var result in typedResults)
            {
                yield return result;
            }
        }

        private async IAsyncEnumerable<PotentialHlaMatchRelation> MatchAtLocus(
            Locus locus,
            MatchingFilteringOptions filteringOptions,
            LocusInfo<IEnumerable<int>> pGroups,
            bool mustBeDoubleMatch,
            DateTime? cutOffDate)
        {
            var sqlMatchResults = MatchAtLocusSql(locus, filteringOptions, pGroups, mustBeDoubleMatch, cutOffDate);
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
            bool mustBeDoubleMatch,
            DateTime? cutOffDate)
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

                    var matchingHlaTableName = MatchingHla.TableName(locus);
                    var hlaPGroupRelationTableName = HlaNamePGroupRelation.TableName(locus);
                    
                    var donorTypeFilteredJoin = filteringOptions.ShouldFilterOnDonorType
                        // ReSharper disable once PossibleInvalidOperationException - implicitly checked via ShouldFilterOnDonorType
                        ? $@"INNER JOIN Donors d ON {selectDonorIdStatement} = d.DonorId AND d.DonorType = {(int) filteringOptions.DonorType}"
                        : "";

                    var donorUpdatedJoin = cutOffDate != null
                        ? $@"INNER JOIN DonorManagementLogs dml ON {selectDonorIdStatement} = dml.DonorId AND dml.LastUpdateDateTime > {cutOffDate}"
                        : "";

                    var donorIdTempTableJoinConfig = SqlTempTableFiltering.PrepareTempTableFiltering("m", "DonorId", filteringOptions.DonorIds, "DonorIds");
                    var pGroups1TempTableJoinConfig = SqlTempTableFiltering.PrepareTempTableFiltering("hlaPGroupRelations", "PGroupId", pGroups.Position1, "PGroups1");
                    var pGroups2TempTableJoinConfig = SqlTempTableFiltering.PrepareTempTableFiltering("hlaPGroupRelations", "PGroupId", pGroups.Position2, "PGroups2");

                    var donorIdTempTableJoin = filteringOptions.ShouldFilterOnDonorIds ? donorIdTempTableJoinConfig.FilteredJoinQueryString : "";

                    var joinType = mustBeDoubleMatch ? "INNER" : "FULL OUTER";

                    var sql = $@"
SELECT * INTO #Pos1 FROM 
(
SELECT DISTINCT m.DonorId as DonorId1, TypePosition as TypePosition1
FROM {hlaPGroupRelationTableName} hlaPGroupRelations
{pGroups1TempTableJoinConfig.FilteredJoinQueryString}
JOIN {matchingHlaTableName} m ON m.HlaNameId = hlaPGroupRelations.HlaNameId
{donorIdTempTableJoin}
{donorUpdatedJoin}
) as m_1; 

CREATE INDEX IX_Temp_Position1 ON #Pos1(DonorId1);

SELECT * INTO #Pos2 FROM 
(
SELECT DISTINCT m.DonorId as DonorId2, TypePosition as TypePosition2
FROM {hlaPGroupRelationTableName} hlaPGroupRelations
{pGroups2TempTableJoinConfig.FilteredJoinQueryString}
JOIN {matchingHlaTableName} m ON m.HlaNameId = hlaPGroupRelations.HlaNameId
{donorIdTempTableJoin}
{donorUpdatedJoin}
) as m_2;

CREATE INDEX IX_Temp_Position2 ON #Pos2(DonorId2);

SELECT DISTINCT {selectDonorIdStatement} as DonorId, TypePosition1, TypePosition2
FROM #Pos1 as m_1
{joinType} JOIN #Pos2 as m_2 ON DonorId1 = DonorId2
{donorTypeFilteredJoin}
ORDER BY DonorId
";
                    await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
                    {
                        if (filteringOptions.DonorIds != null)
                        {
                            await donorIdTempTableJoinConfig.BuildTempTableFactory(conn);
                        }

                        await pGroups1TempTableJoinConfig.BuildTempTableFactory(conn);
                        await pGroups2TempTableJoinConfig.BuildTempTableFactory(conn);

                        // This is streamed from the database via disabling buffering (the default CommandFlags value = `Buffered`).
                        // This allows us to minimise our memory footprint, by not loading all donors into memory at once, and filtering as we go. 
                        var commandDefinition = new CommandDefinition(sql, commandTimeout: 3600, flags: CommandFlags.None);

                        var matches = await conn.QueryAsync<DonorLocusMatch>(commandDefinition);
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
            using (logger.RunTimed($"Fetched untyped Donor IDs at Locus: {locus}"))
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

            var donorIdTempTableJoinConfig = SqlTempTableFiltering.PrepareTempTableFiltering("d", "DonorId", donorIds);


            var sql = $@"
                SELECT DonorId FROM Donors d 
                {donorIdTempTableJoinConfig.FilteredJoinQueryString}
                WHERE {DonorHlaColumnAtLocus(locus, TypePosition.One)} IS NULL 
                AND {DonorHlaColumnAtLocus(locus, TypePosition.Two)} IS NULL
                ORDER BY DonorId
                ";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                await donorIdTempTableJoinConfig.BuildTempTableFactory(conn);
                return await conn.QueryAsync<int>(sql, commandTimeout: 1800);
            }
        }

        private static string DonorHlaColumnAtLocus(Locus locus, TypePosition position) => $"{locus.ToString().ToUpper()}_{PositionString(position)}";

        private static string PositionString(TypePosition position) => position == TypePosition.One ? "1" : "2";
    }
}