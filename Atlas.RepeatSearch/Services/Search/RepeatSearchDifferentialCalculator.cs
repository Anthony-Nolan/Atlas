using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.Common.Utils.Extensions;

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
        private readonly IDonorReader donorReader;
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;

        public RepeatSearchDifferentialCalculator(
            IDonorReader donorReader,
            ICanonicalResultSetRepository canonicalResultSetRepository)
        {
            this.donorReader = donorReader;
            this.canonicalResultSetRepository = canonicalResultSetRepository;
        }

        public async Task<SearchResultDifferential> CalculateDifferential(
            string originalSearchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate)
        {
            var returnedDonorCodes = results.Select(r => r.DonorCode).ToList();

            var allDonorsUpdatedSinceCutoff = await donorReader.GetDonorIdsUpdatedSince(searchCutoffDate);

            var nonMatchingDonors = allDonorsUpdatedSinceCutoff.Keys.Except(returnedDonorCodes);

            var previousCanonicalDonors = (await canonicalResultSetRepository.GetCanonicalResults(originalSearchRequestId))
                .Select(r => r.ExternalDonorCode).ToList();

            var newDonors = returnedDonorCodes.Except(previousCanonicalDonors).ToList();
            var updatedDonors = returnedDonorCodes.Except(newDonors).ToList();
            var noLongerMatchingDonors = previousCanonicalDonors.Intersect(nonMatchingDonors).ToList();

            var previousCanonicalDonorsInDonorStore = await donorReader.GetDonorsByExternalDonorCodes(previousCanonicalDonors);
            var deletedDonors = previousCanonicalDonors.Where(d => !previousCanonicalDonorsInDonorStore.ContainsKey(d)).ToList();

            var donorLookup =
                allDonorsUpdatedSinceCutoff.Merge(previousCanonicalDonorsInDonorStore.ToDictionary(d => d.Key, d => d.Value.AtlasDonorId));

            return new SearchResultDifferential
            {
                NewResults = newDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, donorLookup, results)).ToList(),
                UpdatedResults = updatedDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, donorLookup, results)).ToList(),
                RemovedResults = noLongerMatchingDonors.Concat(deletedDonors).ToList()
            };
        }

        private static DonorIdPair LookupDonorIdFromCode(string externalDonorCode, IDictionary<string, int> allDonorsUpdatedSinceCutoff, List<MatchingAlgorithmResult> results)
        {
            return new DonorIdPair
            {
                ExternalDonorCode = externalDonorCode,
                AtlasId = allDonorsUpdatedSinceCutoff.TryGetValue(externalDonorCode, out var atlasId)
                    ? atlasId
                    : results.First(r => r.DonorCode == externalDonorCode).AtlasDonorId
            };
        }
    }
}