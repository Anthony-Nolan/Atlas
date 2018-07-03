using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public interface IHlaUpdateService
    {
        Task UpdateDonorHla();
    }

    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly ILogger logger;
        private readonly IMatchingDictionaryRepository matchingDictionaryRepository;
        private readonly IAntigenCachingService antigenCachingService;

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService,
            IDonorInspectionRepository donorInspectionRepository,
            IDonorImportRepository donorImportRepository,
            ILogger logger,
            IMatchingDictionaryRepository matchingDictionaryRepository,
            IAntigenCachingService antigenCachingService
        )
        {
            this.lookupService = lookupService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
            this.logger = logger;
            this.matchingDictionaryRepository = matchingDictionaryRepository;
            this.antigenCachingService = antigenCachingService;
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

                var inputDonors = (await Task.WhenAll(resultsBatch.Select(FetchDonorHlaData))).Where(x => x != null);
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
            await matchingDictionaryRepository.ConnectToCloudTable();

            // We set up a new matches table each time the job is run - this must be done upfront to avoid multiple tasks setting it up asynchronously
            donorImportRepository.SetupForHlaRefresh();

            // All antigens are fetched from the HLA service. We use our cache for nmdp lookups to avoid too much load on the hla service
            await antigenCachingService.GenerateAntigenCache();

            // P Groups are inserted (when using relational database storage) upfront. All groups are extracted from the matching dictionary, and new ones added to the SQL database
            var pGroups = matchingDictionaryRepository.GetAllPGroups();
            donorImportRepository.InsertPGroups(pGroups);
        }

        private async Task<InputDonor> FetchDonorHlaData(DonorResult donor)
        {
            try
            {
                return new InputDonor
                {
                    DonorId = donor.DonorId,
                    DonorType = donor.DonorType,
                    RegistryCode = donor.RegistryCode,
                    MatchingHla = await donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n))
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

        private async Task<ExpandedHla> Lookup(Locus locus, string hla)
        {
            if (locus.Equals(Locus.Dpb1))
            {
                // TODO:NOVA-1300 figure out how best to lookup matches for Dpb1
                return null;
            }

            return hla == null
                ? null
                : (await lookupService.GetMatchingHla(locus.ToMatchLocus(), hla)).ToExpandedHla(hla);
        }
    }
}