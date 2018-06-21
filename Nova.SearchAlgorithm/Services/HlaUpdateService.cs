using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
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

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService,
            IDonorInspectionRepository donorInspectionRepository, IDonorImportRepository donorImportRepository,
            ILogger logger)
        {
            this.lookupService = lookupService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.donorImportRepository = donorImportRepository;
            this.logger = logger;
        }

        public async Task UpdateDonorHla()
        {
            var batch = donorInspectionRepository.AllDonors();
            var totalUpdated = 0;
            var stopwatch = new Stopwatch();

            while (batch.HasMoreResults)
            {
                stopwatch.Reset();
                stopwatch.Start();
                var results = (await batch.RequestNextAsync()).ToList();

                // The outer batch size is set by the storage implementation, and is 1000 for Azure Tables
                // The inner batch is currently necessary to get insights within a reasonable timeframe
                const int parallelBatchSize = 5;
                var parallelBatchNumber = 0;
                while (results.Skip(parallelBatchNumber * parallelBatchSize).Any())
                {
                    await Task.WhenAll(
                        results
                            .Skip(parallelBatchNumber * parallelBatchSize)
                            .Take(parallelBatchSize)
                            .Select(UpdateSingleDonorHlaAsync)
                    ).ConfigureAwait(false);
                    
                    parallelBatchNumber++;

                    stopwatch.Stop();
                    totalUpdated += parallelBatchNumber * parallelBatchSize;
                    logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
                    {
                        {"NumberOfDonors", totalUpdated.ToString()},
                        {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
                    });
                }
            }
        }

        private async Task UpdateSingleDonorHlaAsync(DonorResult donor)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var update = new InputDonor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                MatchingHla = await donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n))
            };
            var timeForHlaFetch = stopwatch.ElapsedMilliseconds;

            logger.SendTrace("Fetched Hla Data", LogLevel.Info, new Dictionary<string, string>
            {
                {"DonorId", donor.DonorId.ToString()},
                {"HlaFetchTime", timeForHlaFetch.ToString()},
            });

            await donorImportRepository.RefreshMatchingGroupsForExistingDonor(update);

            var totalTime = stopwatch.ElapsedMilliseconds;
            var metrics = new Dictionary<string, string>
            {
                {"DonorId", donor.DonorId.ToString()},
                {"NumberOfHla", donor.HlaNames.ToEnumerable().Count(hla => hla != null).ToString()},
                {"TotalTime", totalTime.ToString()},
                {"HlaFetchTime", timeForHlaFetch.ToString()},
                {"RefreshTime", (totalTime - timeForHlaFetch).ToString()},
            };

            logger.SendTrace("Refreshed Donor Hla Matching Groups", LogLevel.Info, metrics);
        }

        private async Task<ExpandedHla> Lookup(Locus locus, string hla)
        {
            return hla == null 
                ? null
                : (await lookupService.GetMatchingHla(locus.ToMatchLocus(), hla)).ToExpandedHla(hla);
        }
    }
}