using System.Web.Http;
using Nova.SearchAlgorithmService.Services;

namespace Nova.SearchAlgorithmService.Controllers
{
    public class ServiceStatusController : ApiController
    {
        private readonly IServiceStatusService serviceStatusService;

        public ServiceStatusController(IServiceStatusService statusService)
        {
            serviceStatusService = statusService;
        }

        [Route("service-status")]
        public IHttpActionResult GetServiceStatus()
        {
            var status = serviceStatusService.GetServiceStatus();
            return Ok(status);
        }
    }
}
