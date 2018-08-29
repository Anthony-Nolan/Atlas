using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.DonorService.Client;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        Task StartDonorImport();
    }

    public class DonorImportService : IDonorImportService
    {
        private const int DonorPageSize = 100;

        private readonly IDonorInspectionRepository donorInspectionRespository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly ILogger logger;

        public DonorImportService(
            IDonorInspectionRepository donorInspectionRespository,
            IDonorImportRepository donorImportRepository,
            IDonorServiceClient donorServiceClient,
            ILogger logger)
        {
            this.donorInspectionRespository = donorInspectionRespository;
            this.donorImportRepository = donorImportRepository;
            this.donorServiceClient = donorServiceClient;
            this.logger = logger;
        }

        public async Task StartDonorImport()
        {
            try
            {
                await ContinueDonorImport(await donorInspectionRespository.HighestDonorId());
            }
            catch (Exception ex)
            {
                throw new DonorImportHttpException("Unable to complete donor import: " + ex.Message, ex);
            }
        }

        public async Task ContinueDonorImport(int lastId)
        {
            var nextId = lastId;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
            var page = await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId);
            
            while (page.DonorsInfo.Any())
            {
                await donorImportRepository.InsertBatchOfDonors(page.DonorsInfo.Select(d => d.ToRawImportDonor()));

                stopwatch.Stop();
                logger.SendTrace("Imported donor batch", LogLevel.Info, new Dictionary<string, string>
                {
                    { "BatchSize", DonorPageSize.ToString() },
                    { "BatchImportTime", stopwatch.ElapsedMilliseconds.ToString() },
                });           
                stopwatch.Reset();
                stopwatch.Start();
                
                logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
                nextId = page.LastId ?? (await donorInspectionRespository.HighestDonorId());
                page = await donorServiceClient.GetDonorsInfoForSearchAlgorithm(DonorPageSize, nextId);
            }
            
            logger.SendTrace("Donor import is complete", LogLevel.Info);
        }
    }
}