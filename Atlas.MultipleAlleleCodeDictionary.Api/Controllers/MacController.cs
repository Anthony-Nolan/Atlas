using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Atlas.MultipleAlleleCodeDictionary.MacImportService;

namespace Atlas.MultipleAlleleCodeDictionary.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MacController : ControllerBase
    {
        [HttpGet]
        public OkResult Get()
        {
            var importer = new MacImporter();
            importer.ImportLatestMultipleAlleleCodes();
            return Ok();
        }
    }
}