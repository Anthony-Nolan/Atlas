using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    internal interface IPerLocusDonorMatchingService
    {
        /// <summary>
        /// Runs SQL matching for this locus, and consolidates all relations results by donor id, to return a <see cref="LocusMatchDetails"/> object for each donor.
        ///
        /// Only returns donors with *at least one match* at this locus, even if two mismatches are allowed (to do otherwise would be to return all donors!)
        /// 
        /// Returns a tuple of DonorId to LocusMatchDetails, to allow donor identification of the locus result.
        /// <param name="locus">The locus to perform filtering on.</param>
        /// <param name="criteria">Search criteria at this locus - covers p-groups to match, and number of allowed mismatches.</param>
        /// <param name="searchType">Search donor type. May be used to filter donors - but this is not guaranteed, so donor type filtering should not be assumed from this class.</param>
        /// <param name="donorIds">If provided, only donors contained in this list of ids will be returned.</param>
        /// </summary>
        IAsyncEnumerable<(int, LocusMatchDetails)> FindMatchesAtLocus(
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            DonorType searchType,
            HashSet<int> donorIds = null);
    }

    internal class PerLocusDonorMatchingService : IPerLocusDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IDatabaseFilteringAnalyser databaseFilteringAnalyser;
        private readonly ILogger searchLogger;
        private readonly IPGroupRepository pGroupRepository;

        public PerLocusDonorMatchingService(
            IActiveRepositoryFactory repositoryFactory,
            ILogger searchLogger,
            IDatabaseFilteringAnalyser databaseFilteringAnalyser)
        {
            donorSearchRepository = repositoryFactory.GetDonorSearchRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.searchLogger = searchLogger;
            this.databaseFilteringAnalyser = databaseFilteringAnalyser;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<(int, LocusMatchDetails)> FindMatchesAtLocus(
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            DonorType searchType,
            HashSet<int> donorIds = null)
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchDonorType = searchType,
                PGroupIdsToMatchInPositionOne = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionOne),
                PGroupIdsToMatchInPositionTwo = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionTwo),
                MismatchCount = criteria.MismatchCount,
            };

            var filteringOptions = new MatchingFilteringOptions
            {
                DonorType = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(repoCriteria) ? searchType : (DonorType?) null,
                DonorIds = donorIds
            };

            var donorMatchRelations = donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria, filteringOptions);

            (int, LocusMatchDetails) aggregatedDonorRelations = default;

            // relies on repository returning donors in groups by donor id
            await foreach (var relation in donorMatchRelations)
            {
                var positionPair = (relation.SearchTypePosition, relation.MatchingTypePosition);
                if (aggregatedDonorRelations == default)
                {
                    aggregatedDonorRelations = (relation.DonorId, new LocusMatchDetails
                    {
                        PositionPairs = new HashSet<(LocusPosition, LocusPosition)> {positionPair}
                    });
                    continue;
                }

                // current relation for same donor as previous, so we add to the current aggregate
                if (aggregatedDonorRelations.Item1 == relation.DonorId)
                {
                    aggregatedDonorRelations.Item2.PositionPairs.Add(positionPair);
                }
                // current relation is for new donor - so previous donor is complete and can be returned
                else
                {
                    yield return aggregatedDonorRelations;
                    aggregatedDonorRelations = (relation.DonorId, new LocusMatchDetails
                    {
                        PositionPairs = new HashSet<(LocusPosition, LocusPosition)> {positionPair}
                    });
                }
            }

            // Will still be uninitialised if no results for locus, in which case we don't want to return null.
            // If there are any results, this ensures that the final donor's results are returned.
            if (aggregatedDonorRelations != default)
            {
                yield return aggregatedDonorRelations;
            }
        }
    }
}