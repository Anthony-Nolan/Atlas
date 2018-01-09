using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithmService.Client.Models;
using Nova.SearchAlgorithmService.Services;

namespace Nova.SearchAlgorithmService.Controllers
{
    [RoutePrefix("api/templates")]
    public class TemplateController : ApiController
    {
        private readonly ITemplateService templateService;

        public TemplateController(ITemplateService service)
        {
            templateService = service;
        }

        [Route("{id}")]
        public Task<TemplateResponseModel> Get(string id)
        {
            return templateService.Get(id);
        }
    }
}
