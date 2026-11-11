using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.Common.ApplicationInsights;
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
            ResultsPipelineTimings.SetMetric("CutoffAgeDays", (DateTimeOffset.UtcNow - searchCutoffDate).TotalDays);

            IReadOnlyDictionary<string, int> allDonorsUpdatedSinceCutoff;
            using (ResultsPipelineTimings.Time("GetDonorIdsUpdatedSince"))
            {
                allDonorsUpdatedSinceCutoff = await donorReader.GetDonorIdsUpdatedSince(searchCutoffDate);
            }
            ResultsPipelineTimings.SetMetric("DonorsUpdatedSinceCutoff", allDonorsUpdatedSinceCutoff.Count);

            var nonMatchingDonors = allDonorsUpdatedSinceCutoff.Keys.Except(returnedDonorCodes);

            List<string> previousCanonicalDonors;
            using (ResultsPipelineTimings.Time("GetCanonicalResults"))
            {
                previousCanonicalDonors = (await canonicalResultSetRepository.GetCanonicalResults(originalSearchRequestId))
                    .Select(r => r.ExternalDonorCode).ToList();
            }
            ResultsPipelineTimings.SetMetric("CanonicalDonors", previousCanonicalDonors.Count);

            List<string> newDonors, updatedDonors, noLongerMatchingDonors;
            using (ResultsPipelineTimings.Time("DiffInMemory"))
            {
                newDonors = returnedDonorCodes.Except(previousCanonicalDonors).ToList();
                updatedDonors = returnedDonorCodes.Except(newDonors).ToList();
                noLongerMatchingDonors = previousCanonicalDonors.Intersect(nonMatchingDonors).ToList();
            }

            IReadOnlyDictionary<string, Donor> previousCanonicalDonorsInDonorStore;
            using (ResultsPipelineTimings.Time("GetDonorsByExternalDonorCodes"))
            {
                previousCanonicalDonorsInDonorStore = await donorReader.GetDonorsByExternalDonorCodes(previousCanonicalDonors);
            }

            var deletedDonors = previousCanonicalDonors.Where(d => !previousCanonicalDonorsInDonorStore.ContainsKey(d)).ToList();
            ResultsPipelineTimings.SetMetric("DeletedDonors", deletedDonors.Count);

            var atlasIdsByDonorCode = results.ToDictionary(r => r.DonorCode, r => r.AtlasDonorId);

            return new SearchResultDifferential
            {
                NewResults = newDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, atlasIdsByDonorCode)).ToList(),
                UpdatedResults = updatedDonors.Select(donorCode => LookupDonorIdFromCode(donorCode, atlasIdsByDonorCode)).ToList(),
                RemovedResults = noLongerMatchingDonors.Concat(deletedDonors).ToList()
            };
        }

        private static DonorIdPair LookupDonorIdFromCode(string externalDonorCode, IReadOnlyDictionary<string, int> atlasIdByDonorCode)
        {
            return new DonorIdPair
            {
                ExternalDonorCode = externalDonorCode,
                AtlasId = atlasIdByDonorCode[externalDonorCode]
            };
        }
    }
}