using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.DonorService.Client;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Exceptions;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public class DonorImportService : IDonorImportService
    {
        private const int DonorPageSize = 500;

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

            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
            var page = await donorServiceClient.GetDonors(DonorPageSize, nextId);

            while (page.Donors.Any())
            {
                await Task.WhenAll(page.Donors.Select(InsertDonor));

                logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {nextId} onwards", LogLevel.Trace);
                nextId = page.LastId ?? (await donorInspectionRespository.HighestDonorId());
                page = await donorServiceClient.GetDonors(DonorPageSize, nextId);
            }

            logger.SendTrace("Donor import is complete", LogLevel.Info);
        }

        private async Task InsertDonor(Donor donor)
        {
            try
            {
                await donorImportRepository.InsertDonor(donor.ToRawImportDonor());
            }
            catch (Exception e)
            {
                // Log the error clearly so we can grep them out and import... manually?
                var message = $"Failed to import donor {donor.DonorId} with reason {e.Message}";
                Trace.WriteLine(message);
                logger.SendTrace(message, LogLevel.Error);
                throw new DonorImportException(message, e);
            }
        }
    }
}