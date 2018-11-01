using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("donor")]
    public class DonorController : ApiController
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
    }
}
