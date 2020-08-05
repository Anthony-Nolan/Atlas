using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.Settings;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    internal interface IFrequencySetStreamer
    {
        Task<Stream> GetFileContents(string fileName);
    }

    internal class FrequencySetStreamer : BlobStreamer, IFrequencySetStreamer
    {
        private readonly string containerName;

        public FrequencySetStreamer(IOptions<MatchPredictionAzureStorageSettings> settings) : base(settings.Value.ConnectionString)
        {
            containerName = settings.Value.HaplotypeFrequencySetBlobContainer;
        }

        public async Task<Stream> GetFileContents(string fileName)
        {
            return await GetContentStream(containerName, fileName);
        }
    }
}