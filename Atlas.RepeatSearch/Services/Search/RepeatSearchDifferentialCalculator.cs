using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.RepeatSearch.Data.Repositories;

namespace Atlas.RepeatSearch.Services.Search
{
    public class SearchResultDifferential
    {
        public List<int> NewResultsDonorIds { get; set; }
        public List<int> UpdatedResultsDonorIds { get; set; }
        public List<DonorIdPair> RemovedDonors { get; set; }
    }

    public interface IRepeatSearchDifferentialCalculator
    {
        Task<SearchResultDifferential> CalculateDifferential(
            string originalSearchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate);
    }

    internal class RepeatSearchDifferentialCalculator : IRepeatSearchDifferentialCalculator
    {
        private readonly IDonorReadRepository donorReadRepository;
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;

        public RepeatSearchDifferentialCalculator(
            IDonorReadRepository donorReadRepository,
            ICanonicalResultSetRepository canonicalResultSetRepository)
        {
            this.donorReadRepository = donorReadRepository;
            this.canonicalResultSetRepository = canonicalResultSetRepository;
        }

        public async Task<SearchResultDifferential> CalculateDifferential(
            string originalSearchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate)
        {
            var returnedDonorIds = results.Select(r => r.AtlasDonorId).ToList();

            // (a) 
            var allDonorsUpdatedSinceCutoff = await donorReadRepository.GetDonorIdsUpdatedSince(searchCutoffDate);
            var donorCodeLookup = allDonorsUpdatedSinceCutoff.ToDictionary(x => x.Value, x => x.Key);

            // (b) = results

            // (c) = (a) - (b)
            var nonMatchingDonors = allDonorsUpdatedSinceCutoff.Values.Except(returnedDonorIds);

            // (d) 
            var previousCanonicalDonors = (await canonicalResultSetRepository.GetCanonicalResults(originalSearchRequestId))
                .Select(r => r.AtlasDonorId).ToList();

            var newDonors = returnedDonorIds.Except(previousCanonicalDonors).ToList();
            var updatedDonors = returnedDonorIds.Except(newDonors).ToList();
            var removedDonors = previousCanonicalDonors.Intersect(nonMatchingDonors).ToList();

            // TODO: ATLAS-861: calculate which donors have been deleted and therefore don't show in (a) but still shouldn't be in the canonical set
            
            return new SearchResultDifferential
            {
                NewResultsDonorIds = newDonors,
                UpdatedResultsDonorIds = updatedDonors,
                RemovedDonors = removedDonors.Select(atlasId => new DonorIdPair
                {
                    AtlasId = atlasId,
                    ExternalDonorCode = donorCodeLookup[atlasId]
                }).ToList()
            };
        }
    }
}