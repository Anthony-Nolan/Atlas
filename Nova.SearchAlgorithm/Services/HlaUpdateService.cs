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

        public HlaUpdateService(IMatchingDictionaryLookupService lookupService, IDonorInspectionRepository donorInspectionRepository, IDonorImportRepository donorImportRepository, ILogger logger)
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
                var results = await batch.RequestNextAsync();

                
                // TODO: NOVA-1295 Find out if this works with Task.WhenAll() 
                foreach (var donor in results)
                {
                    await UpdateSingleDonorHlaAsync(donor);
                }

                stopwatch.Stop();
                totalUpdated += results.Count();
                logger.SendTrace("Updated Donors", LogLevel.Info, new Dictionary<string, string>
                {
                    { "NumberOfDonors", totalUpdated.ToString() },
                    { "UpdateTime", stopwatch.ElapsedMilliseconds.ToString() }
                });

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
                MatchingHla = (await donor.HlaNames
                                  .WhenAllPositions((l, p, n) => n == null ? Task.FromResult((IMatchingHlaLookupResult) null) : lookupService.GetMatchingHla(l.ToMatchLocus(), n))
                              ).Map((l, p, n) => n?.ToExpandedHla())
            };
            var timeForHlaFetch = stopwatch.ElapsedMilliseconds;

            await donorImportRepository.RefreshMatchingGroupsForExistingDonor(update);

            var totalTime = stopwatch.ElapsedMilliseconds;
            logger.SendTrace("Refreshed Donor Hla Matching Groups", LogLevel.Info, new Dictionary<string, string>
            {
                { "DonorId", donor.DonorId.ToString() },
                { "NumberOfHla", donor.HlaNames.ToEnumerable().Count(hla => hla != null).ToString() },
                { "TotalTime", totalTime.ToString() },
                { "HlaFetchTime", timeForHlaFetch.ToString() },
                { "RefreshTime",  (totalTime - timeForHlaFetch).ToString() },
            });
        }
    }
}