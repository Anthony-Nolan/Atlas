using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    internal interface IFrequencySetStreamer
    {
        Task<Stream> GetFileContents(string fileName);
    }

    internal class FrequencySetStreamer : AzureStorageBlobClient, IFrequencySetStreamer
    {
        private readonly string containerName;

        public FrequencySetStreamer(IOptions<MatchPredictionAzureStorageSettings> settings, ILogger logger)
            : base(settings.Value.ConnectionString, logger)
        {
            containerName = settings.Value.FrequencySetManualImportBlobContainer;
        }

        public async Task<Stream> GetFileContents(string fileName)
        {
            return await GetContentStream(containerName, fileName);
        }
    }
}
