using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public class HlaUpdateService : IHlaUpdateService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly ILogger logger;
        private readonly IMatchingDictionaryRepository matchingDictionaryRepository;

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService,
            IDonorInspectionRepository donorInspectionRepository,
            IDonorImportRepository donorImportRepository,
            ILogger logger,
            IMatchingDictionaryRepository matchingDictionaryRepository)
        {
            this.lookupService = lookupService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
            this.logger = logger;
            this.matchingDictionaryRepository = matchingDictionaryRepository;
        }

        public async Task UpdateDonorHla()
        {
            var batchedQuery = donorInspectionRepository.AllDonors();
            var totalUpdated = 0;
            var stopwatch = new Stopwatch();

            // Cloud tables are cached for performance reasons - this must be done upfront to avoid multiple tasks attempting to set up the cache
            await matchingDictionaryRepository.ConnectToCloudTable();
            // We set up a new matches table each time the job is run - this must be done upfront to avoid multiple tasks setting it up asynchronously
            donorImportRepository.SetupForHlaRefresh();

            var pGroups = matchingDictionaryRepository.GetAllPGroups();
            donorImportRepository.InsertPGroups(pGroups);
            while (batchedQuery.HasMoreResults)
            {
                stopwatch.Start();
                var resultsBatch = (await batchedQuery.RequestNextAsync()).ToList();

                // The outer batch size is set by the storage implementation, and is 1000 for Azure Tables
                // The inner batch is currently necessary to get insights within a reasonable timeframe
                const int parallelBatchSize = 100;
                foreach (var subBatch in resultsBatch.Batch(parallelBatchSize))
                {
                    stopwatch.Restart();

                    var inputDonors = await Task.WhenAll(subBatch.Select(FetchDonorHlaData));
                    await donorImportRepository.RefreshMatchingGroupsForExistingDonorBatch(inputDonors);
                    
                    stopwatch.Stop();
                    totalUpdated += parallelBatchSize;
                    logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
                    {
                        {"NumberOfDonors", totalUpdated.ToString()},
                        {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
                    });
                }
            }
        }

        private async Task<InputDonor> FetchDonorHlaData(DonorResult donor)
        {
            return new InputDonor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                MatchingHla = await donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n))
            };
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