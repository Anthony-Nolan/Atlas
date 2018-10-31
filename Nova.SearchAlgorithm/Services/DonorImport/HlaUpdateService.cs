using System;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.SearchAlgorithm.Services.DonorImport
{
    public interface IHlaUpdateService
    {
        Task UpdateDonorHla();
    }

    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly ILogger logger;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IPGroupRepository pGroupRepository;

        public HlaUpdateService(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IDonorInspectionRepository donorInspectionRepository,
            IDonorImportRepository donorImportRepository,
            ILogger logger,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAntigenCachingService antigenCachingService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IPGroupRepository pGroupRepository
        )
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
            this.logger = logger;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.antigenCachingService = antigenCachingService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.pGroupRepository = pGroupRepository;
        }

        public async Task UpdateDonorHla()
        {
            try
            {
                await PerformUpfrontSetup();
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshSetUpFailureEventModel(e));
                throw;
            }

            try
            {
                var batchedQuery = donorInspectionRepository.AllDonors();
                while (batchedQuery.HasMoreResults)
                {
                    var donorBatch = await batchedQuery.RequestNextAsync();
                    await UpdateDonorBatch(donorBatch.ToList());
                }
            }
            catch (Exception e)
            {
                logger.SendEvent(new HlaRefreshFailureEventModel(e));
                throw;
            }
        }

        private async Task UpdateDonorBatch(IEnumerable<DonorResult> donorBatch)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var donorHlaData = await Task.WhenAll(donorBatch.Select(FetchDonorHlaData));
            var inputDonors = donorHlaData.Where(x => x != null).ToList();
            await donorImportRepository.RefreshMatchingGroupsForExistingDonorBatch(inputDonors);

            stopwatch.Stop();
            logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
            {
                {"NumberOfDonors", inputDonors.Count().ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
        }

        private async Task PerformUpfrontSetup()
        {
            // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
            await hlaMatchingLookupRepository.LoadDataIntoMemory();
            await alleleNamesLookupRepository.LoadDataIntoMemory();

            // We set up a new matches table each time the job is run - this must be done upfront to avoid multiple tasks setting it up asynchronously
            donorImportRepository.SetupForHlaRefresh();

            // All antigens are fetched from the HLA service. We use our cache for nmdp lookups to avoid too much load on the hla service
            await antigenCachingService.GenerateAntigenCache();

            // P Groups are inserted (when using relational database storage) upfront. All groups are extracted from the matching dictionary, and new ones added to the SQL database
            var pGroups = hlaMatchingLookupRepository.GetAllPGroups();
            pGroupRepository.InsertPGroups(pGroups);
        }

        private async Task<InputDonor> FetchDonorHlaData(DonorResult donor)
        {
            try
            {
                var matchingHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(donor.HlaNames);

                return new InputDonor
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = matchingHla
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