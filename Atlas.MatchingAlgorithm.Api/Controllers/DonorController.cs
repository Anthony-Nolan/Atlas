using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
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
        private readonly IActiveHlaNomenclatureVersionAccessor hlaVersionAccessor;

        public DonorController(
            IDonorService donorService,
            IActiveDatabaseProvider activeDatabaseProvider,
            IActiveHlaNomenclatureVersionAccessor hlaVersionAccessor)
        {
            this.donorService = donorService;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.hlaVersionAccessor = hlaVersionAccessor;
        }

        [HttpPut]
        [Route("batch")]
        public async Task CreateOrUpdateDonorBatch([FromBody] DonorInfoBatch donorBatch)
        {
            await donorService.CreateOrUpdateDonorBatch(
                donorBatch.Donors,
                activeDatabaseProvider.GetActiveDatabase(),
                hlaVersionAccessor.GetActiveHlaNomenclatureVersion());
        }
    }
}
