using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Controllers
{
    public class TestHarnessController : ControllerBase
    {
        private readonly IMacExpander macExpander;
        private readonly ITestHarnessGenerator testHarnessGenerator;

        public TestHarnessController(ITestHarnessGenerator testHarnessGenerator, IMacExpander macExpander)
        {
            this.testHarnessGenerator = testHarnessGenerator;
            this.macExpander = macExpander;
        }

        [HttpPost]
        [Route("expanded-macs")]
        public async Task ExpandGenericMacs()
        {
            try
            {
                await macExpander.ExpandLatestGenericMacs();

            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to complete latest MAC expansion.", ex);
            }
        }

        [HttpPost]
        [Route("test-harness")]
        public async Task<int> GenerateTestHarness([FromBody] GenerateTestHarnessRequest request)
        {
            try
            {
                new GenerateTestHarnessRequestValidator().ValidateAndThrow(request);
                return await testHarnessGenerator.GenerateTestHarness(request);
            }
            catch (ValidationException ex)
            {
                throw new AtlasHttpException(HttpStatusCode.BadRequest, "Invalid test harness generation request.", ex);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to complete Test Harness generation.", ex);
            }
        }
    }
}