using System.IO;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    internal interface ISearchResultsStreamer
    {
        Task<Stream> GetSearchResultsBlobContents(string containerName, string blobName);
    }

    internal class SearchResultsStreamer : BlobStreamer, ISearchResultsStreamer
    {
        public SearchResultsStreamer(IOptions<VerificationAzureStorageSettings> settings) : base(settings.Value.ConnectionString)
        {
        }

        public async Task<Stream> GetSearchResultsBlobContents(string containerName, string blobName)
        {
            return await GetContentStream(containerName, blobName);
        }
    }
}