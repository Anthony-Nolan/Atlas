using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Test.Validation.Functions
{
    /// <summary>
    /// Functions required for running of either exercise.
    /// </summary>
    public class ValidationFunctions
    {
        private readonly ISubjectInfoImporter subjectInfoImporter;

        public ValidationFunctions(ISubjectInfoImporter subjectInfoImporter)
        {
            this.subjectInfoImporter = subjectInfoImporter;
        }

        [FunctionName($"0_{nameof(ImportSubjects)}")]
        public async Task ImportSubjects(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ImportRequest), nameof(ImportRequest))]
            HttpRequest request)
        {
            try
            {
                var importRequest = JsonConvert.DeserializeObject<ImportRequest>(await new StreamReader(request.Body).ReadToEndAsync());
                await subjectInfoImporter.Import(importRequest);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to import subjects.", ex);
            }
        }
    }
}
