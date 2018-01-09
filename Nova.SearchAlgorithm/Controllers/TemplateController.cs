using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
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
