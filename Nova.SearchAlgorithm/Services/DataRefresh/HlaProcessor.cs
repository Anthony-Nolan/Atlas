using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IHlaProcessor
    {
        /// <summary>
        /// For any donors with a higher id than the last updated donor:
        ///  - Fetches p-groups for all donor's hla
        ///  - Stores the pre-processed p-groups for use in matching
        /// </summary>
        Task UpdateDonorHla(string hlaDatabaseVersion);
    }

    public class HlaProcessor : IHlaProcessor
    {
        private readonly ILogger logger;
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDataRefreshRepository repository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaProcessor(
            ILogger logger,
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IAntigenCachingService antigenCachingService,
            IDonorImportRepository donorImportRepository,
            IDataRefreshRepository repository,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IPGroupRepository pGroupRepository)
        {
            this.logger = logger;
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.antigenCachingService = antigenCachingService;
            this.donorImportRepository = donorImportRepository;
            this.repository = repository;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.pGroupRepository = pGroupRepository;
        }

        public async Task UpdateDonorHla(string hlaDatabaseVersion)
        {
            await PerformUpfrontSetup(hlaDatabaseVersion);

            try
            {
                await PerformHlaUpdate(hlaDatabaseVersion);
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
            finally
            {
                await PerformTearDown();
            }
        }

        private async Task PerformUpfrontSetup(string hlaDatabaseVersion)
        {
            try
            {
                // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
                await hlaMatchingLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
                await alleleNamesLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);

                // All antigens are fetched from the HLA service. We use our cache for NMDP lookups to avoid too much load on the hla service
                await antigenCachingService.GenerateAntigenCache();

                // P Groups are inserted (when using relational database storage) upfront. All groups are extracted from the matching dictionary, and new ones added to the SQL database
                var pGroups = hlaMatchingLookupRepository.GetAllPGroups();
                pGroupRepository.InsertPGroups(pGroups);

                await donorImportRepository.FullHlaRefreshSetUp();
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }
        }

        private async Task PerformHlaUpdate(string hlaDatabaseVersion)
        {
            var batchedQuery = await repository.DonorsAddedSinceLastHlaUpdate();
            while (batchedQuery.HasMoreResults)
            {
                var donorBatch = (await batchedQuery.RequestNextAsync()).ToList();
                await UpdateDonorBatch(donorBatch, hlaDatabaseVersion);
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<DonorResult> donorBatch, string hlaDatabaseVersion)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var donorHlaData = await Task.WhenAll(donorBatch.Select(d => FetchDonorHlaData(d, hlaDatabaseVersion)));
            var inputDonors = donorHlaData.Where(x => x != null).ToList();
            await donorImportRepository.AddMatchingPGroupsForExistingDonorBatch(inputDonors);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
            {
                {"NumberOfDonors", inputDonors.Count().ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
        }

        private async Task PerformTearDown()
        {
            await donorImportRepository.FullHlaRefreshTearDown();
        }

        private async Task<InputDonorWithExpandedHla> FetchDonorHlaData(DonorResult donor, string hlaDatabaseVersion)
        {
            try
            {
                var matchingHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(donor.HlaNames, hlaDatabaseVersion);

                return new InputDonorWithExpandedHla
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = matchingHla,
                };
            }
            catch (MatchingDictionaryException e)
            {
                logger.SendEvent(new HlaRefreshMatchingDictionaryLookupFailureEventModel(e, donor.DonorId.ToString()));
                return null;
            }
        }
    }
}