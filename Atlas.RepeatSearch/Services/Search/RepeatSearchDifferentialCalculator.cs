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
        public List<DonorIdPair> NewResults { get; set; }
        public List<DonorIdPair> UpdatedResults { get; set; }
        /// <summary>
        /// Returns External Donor IDs only.
        /// Atlas ID will not be needed for Match Prediction for removed donors, and in the case of deleted donors, the code will no longer have an associated Atlas donor id!  
        /// </summary>
        public List<string> RemovedResults { get; set; }
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
            var returnedDonorCodes = results.Select(r => r.ExternalDonorCode).ToList();

            // (a) 
            var allDonorsUpdatedSinceCutoff = await donorReadRepository.GetDonorIdsUpdatedSince(searchCutoffDate);

            // (b) = results

            // (c) = (a) - (b)
            var nonMatchingDonors = allDonorsUpdatedSinceCutoff.Keys.Except(returnedDonorCodes);

            // (d) 
            var previousCanonicalDonors = (await canonicalResultSetRepository.GetCanonicalResults(originalSearchRequestId))
                .Select(r => r.ExternalDonorCode).ToList();

            var newDonors = returnedDonorCodes.Except(previousCanonicalDonors).ToList();
            var updatedDonors = returnedDonorCodes.Except(newDonors).ToList();
            var removedDonors = previousCanonicalDonors.Intersect(nonMatchingDonors).ToList();

            // TODO: ATLAS-861: calculate which donors have been deleted and therefore don't show in (a) but still shouldn't be in the canonical set
            return new SearchResultDifferential
            {
                NewResults = newDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, allDonorsUpdatedSinceCutoff)).ToList(),
                UpdatedResults = updatedDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, allDonorsUpdatedSinceCutoff)).ToList(),
                RemovedResults = removedDonors.ToList()
            };
        }

        private static DonorIdPair LookupDonorIdFromCode(string externalDonorCode, IReadOnlyDictionary<string, int> allDonorsUpdatedSinceCutoff)
        {
            return new DonorIdPair {ExternalDonorCode = externalDonorCode, AtlasId = allDonorsUpdatedSinceCutoff[externalDonorCode]};
        }
    }
}