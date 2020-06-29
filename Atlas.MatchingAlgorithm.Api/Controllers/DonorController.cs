using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.Donors;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("donor")]
    public class DonorController : ControllerBase
    {
        private readonly IDonorService donorService;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;

        public DonorController(IDonorService donorService, IActiveDatabaseProvider activeDatabaseProvider)
        {
            this.donorService = donorService;
            this.activeDatabaseProvider = activeDatabaseProvider;
        }

        [HttpPut]
        [Route("batch")]
        public async Task CreateOrUpdateDonorBatch([FromBody] DonorInfoBatch donorBatch)
        {
            await donorService.CreateOrUpdateDonorBatch(donorBatch.Donors, activeDatabaseProvider.GetActiveDatabase());
        }
    }
}
