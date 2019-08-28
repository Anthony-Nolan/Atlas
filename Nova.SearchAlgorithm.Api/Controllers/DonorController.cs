using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Services.Donors;

namespace Nova.SearchAlgorithm.Api.Controllers
{
    [Route("donor")]
    public class DonorController : ControllerBase
    {
        private readonly IDonorService donorService;

        public DonorController(IDonorService donorService)
        {
            this.donorService = donorService;
        }

        [HttpPut]
        [Route("batch")]
        public async Task<IEnumerable<InputDonor>> CreateOrUpdateDonorBatch([FromBody] InputDonorBatch donorBatch)
        {
            return await donorService.CreateOrUpdateDonorBatch(donorBatch.Donors);
        }
    }
}
