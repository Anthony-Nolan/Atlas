using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Services;

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

        [HttpPost]
        [Route("")]
        public async Task<InputDonor> CreateDonor([FromBody] InputDonor donor)
        {
            return await donorService.CreateDonor(donor);
        }

        [HttpPut]
        [Route("")]
        public async Task<InputDonor> UpdateDonor([FromBody] InputDonor donor)
        {
            return await donorService.UpdateDonor(donor);
        }

        [HttpPost]
        [Route("batch")]
        public async Task<IEnumerable<InputDonor>> CreateDonorBatch([FromBody] InputDonorBatch donorBatch)
        {
            return await donorService.CreateDonorBatch(donorBatch.Donors);
        }

        [HttpPut]
        [Route("batch")]
        public async Task<IEnumerable<InputDonor>> UpdateDonorBatch([FromBody] InputDonorBatch donorBatch)
        {
            return await donorService.UpdateDonorBatch(donorBatch.Donors);
        }
    }
}
