using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Validators;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class TestHarnessFunctions
    {
        private readonly IMacExpander macExpander;
        private readonly ITestHarnessGenerator testHarnessGenerator;

        public TestHarnessFunctions(ITestHarnessGenerator testHarnessGenerator, IMacExpander macExpander)
        {
            this.testHarnessGenerator = testHarnessGenerator;
            this.macExpander = macExpander;
        }

        [FunctionName(nameof(ExpandGenericMacs))]
        public async Task ExpandGenericMacs([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            try
            {
                await macExpander.ExpandAndStoreLatestGenericMacs();

            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to complete latest MAC expansion.", ex);
            }
        }

        [FunctionName(nameof(GenerateTestHarness))]
        public async Task<IActionResult> GenerateTestHarness(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(GenerateTestHarnessRequest), nameof(GenerateTestHarnessRequest))]
            HttpRequest request)
        {
            try
            {
                var harnessRequest = JsonConvert.DeserializeObject<GenerateTestHarnessRequest>(
                    await new StreamReader(request.Body).ReadToEndAsync());

                new GenerateTestHarnessRequestValidator().ValidateAndThrow(harnessRequest);
                var testHarnessId = await testHarnessGenerator.GenerateTestHarness(harnessRequest);

                return new JsonResult(testHarnessId);
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