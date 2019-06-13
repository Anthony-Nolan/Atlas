using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Services.DonorImport;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class DonorImport
    {
        private readonly IDonorImportService donorImportService;
        private readonly IHlaUpdateService hlaUpdateService;

        public DonorImport(IDonorImportService donorImportService, IHlaUpdateService hlaUpdateService)
        {
            this.donorImportService = donorImportService;
            this.hlaUpdateService = hlaUpdateService;
        }

        [FunctionName("RunDonorImport")]
        public async Task RunDonorImport([HttpTrigger] HttpRequest httpRequest)
        {
            await donorImportService.StartDonorImport();
        }

        [FunctionName("ProcessDonorHla")]
        public async Task RunHlaRefresh([HttpTrigger] HttpRequest httpRequest)
        {
            await hlaUpdateService.UpdateDonorHla();
        }
    }
}