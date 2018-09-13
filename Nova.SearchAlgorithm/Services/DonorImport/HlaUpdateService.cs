using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

        public HlaUpdateService(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            IDonorInspectionRepository donorInspectionRepository,
            IDonorImportRepository donorImportRepository,
            ILogger logger,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAntigenCachingService antigenCachingService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository
        )
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
            this.logger = logger;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.antigenCachingService = antigenCachingService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
        }

        public async Task UpdateDonorHla()
        {
            var batchedQuery = donorInspectionRepository.AllDonors();
            var totalUpdated = 0;
            var stopwatch = new Stopwatch();

            await PerformUpfrontSetup();

            while (batchedQuery.HasMoreResults)
            {
                stopwatch.Start();
                var resultsBatch = (await batchedQuery.RequestNextAsync()).ToList();

                stopwatch.Restart();

                var donorHlaData = await Task.WhenAll(resultsBatch.Select(FetchDonorHlaData));
                var inputDonors = donorHlaData.Where(x => x != null);
                await donorImportRepository.RefreshMatchingGroupsForExistingDonorBatch(inputDonors);

                stopwatch.Stop();
                totalUpdated += inputDonors.Count();
                logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
                {
                    {"NumberOfDonors", totalUpdated.ToString()},
                    {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
                });
            }
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
            donorImportRepository.InsertPGroups(pGroups);
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
                logger.SendTrace("Donor Hla Update Failed", LogLevel.Error, new Dictionary<string, string>
                {
                    {"Reason", "Failed to fetch hla from matching dictionary"},
                    {"DonorId", donor.DonorId.ToString()},
                    {"Exception", e.ToString()},
                });
                return null;
            }
        }
    }
}