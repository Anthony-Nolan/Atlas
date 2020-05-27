using Microsoft.AspNetCore.Mvc;
using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Atlas.MultipleAlleleCodeDictionary.utils;

namespace Atlas.MultipleAlleleCodeDictionary.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MacController : ControllerBase
    {
        [HttpGet]
        public OkResult Get()
        {
            var importer = new MacImporter(new MacRepository(), new MacLineParser());
            importer.ImportLatestMultipleAlleleCodes();
            return Ok();
        }
    }
}