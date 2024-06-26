using System.IO;
using System.Threading.Tasks;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Functions
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IFrequencyFileParser fileParser;
        private readonly IHaplotypeFrequencySetTransformer setTransformer;
        private readonly ITransformedSetWriter setWriter;

        public HaplotypeFrequencySetFunctions(
            IFrequencyFileParser fileParser,
            IHaplotypeFrequencySetTransformer setTransformer,
            ITransformedSetWriter setWriter)
        {
            this.fileParser = fileParser;
            this.setTransformer = setTransformer;
            this.setWriter = setWriter;
        }

        [Function(nameof(TransformHaplotypeFrequencySet))]
        public async Task TransformHaplotypeFrequencySet(
            [RequestBodyType(typeof(TransformHaplotypeFrequencySetRequest), nameof(TransformHaplotypeFrequencySetRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var transformSetRequest = JsonConvert.DeserializeObject<TransformHaplotypeFrequencySetRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var set = await GetFrequencySet(transformSetRequest.HaplotypeFrequencySetFilePath);
            var transformedSet = setTransformer.TransformHaplotypeFrequencySet(set, transformSetRequest.FindReplaceHlaNames);
            await setWriter.WriteTransformedSet(transformSetRequest, transformedSet);
        }

        private async Task<FrequencySetFileSchema> GetFrequencySet(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{nameof(filePath)} not found");
            }
            await using var stream = File.OpenRead(filePath);
            return fileParser.GetFrequencies(stream);
        }
    }
}