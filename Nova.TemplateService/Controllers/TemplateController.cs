using System.Threading.Tasks;
using System.Web.Http;
using Nova.TemplateService.Client.Models;
using Nova.TemplateService.Services;

namespace Nova.TemplateService.Controllers
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
