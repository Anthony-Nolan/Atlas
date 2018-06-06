using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.Utils.Http.Exceptions;

namespace Nova.SearchAlgorithm.Controllers
{
    /// <summary>
    /// Endpoints to expose the donor data we are storing, for debugging and verification.
    /// </summary>
    [RoutePrefix("donor")]
    public class DonorController : ApiController
    {
        private readonly IDonorInspectionRepository donorRepository;

        public DonorController(IDonorInspectionRepository donorRepository)
        {
            this.donorRepository = donorRepository;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<DonorResult> GetDonor(int id)
        {
            var donor = donorRepository.GetDonor(id);
            if (donor == null)
            {
                throw new NovaNotFoundException($"No donor available with ID {id}");
            }
            return donor;
        }

        [HttpGet]
        [Route("highest-donor-id")]
        public Task<int> GetHighestDonorId()
        {
            return donorRepository.HighestDonorId();
        }
    }
}
