using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Settings;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImportFailuresCleaner
    {
        Task DeleteExpiredDonorImportFailures();
    }

    internal class DonorImportFailuresCleaner : IDonorImportFailuresCleaner
    {
        private readonly int donorImportFailuresExpiryInDays;
        private readonly IDonorImportFailureRepository donorImportFailureRepository;

        public DonorImportFailuresCleaner(IDonorImportFailureRepository donorImportFailureRepository, FailureLogsSettings failureLogsSettings)
        {
            this.donorImportFailureRepository = donorImportFailureRepository;
            donorImportFailuresExpiryInDays = failureLogsSettings.ExpiryInDays;
        }

        public async Task DeleteExpiredDonorImportFailures()
        {
            var cutOffDate = DateTimeOffset.Now.AddDays(-1 * donorImportFailuresExpiryInDays);
            await donorImportFailureRepository.DeleteDonorImportFailuresBefore(cutOffDate);
        }
    }
}
