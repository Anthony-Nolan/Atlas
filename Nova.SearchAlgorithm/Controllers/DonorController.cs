using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    /// <summary>
    /// Endpoints to expose the donor data we are storing, for debugging and verification.
    /// </summary>
    [RoutePrefix("donor")]
    public class DonorController : ApiController
    {
        private readonly IDonorMatchRepository donorRepository;

        public DonorController(IDonorMatchRepository donorRepository)
        {
            this.donorRepository = donorRepository;
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetDonor(int id)
        {
            var donor = donorRepository.GetDonor(id);
            if (donor == null)
            {
                return NotFound();
            }
            return Ok(donor);
        }
    }
}
