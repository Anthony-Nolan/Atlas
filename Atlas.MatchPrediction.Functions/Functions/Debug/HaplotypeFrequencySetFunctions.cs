using System;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions.Debug
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;

        public HaplotypeFrequencySetFunctions(IHaplotypeFrequencyService haplotypeFrequencyService)
        {
            this.haplotypeFrequencyService = haplotypeFrequencyService;
        }
        
        [FunctionName(nameof(ImportLocalHaplotypeFrequencySet))]
        public async Task ImportLocalHaplotypeFrequencySet(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ImportLocalHaplotypeFrequencySetRequest), nameof(ImportLocalHaplotypeFrequencySet))]
            HttpRequest httpRequest)
        {
            var request = JsonConvert.DeserializeObject<ImportLocalHaplotypeFrequencySetRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());

            using (var file = new FrequencySetFile
            {
                Contents = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read),
                FileName = new FileInfo(request.FilePath).Name,
                UploadedDateTime = DateTimeOffset.UtcNow
            })
            {
                await haplotypeFrequencyService.ImportFrequencySet(file);
            }
        }
    }

    public class ImportLocalHaplotypeFrequencySetRequest
    {
        public string FilePath { get; set; }
    }
}