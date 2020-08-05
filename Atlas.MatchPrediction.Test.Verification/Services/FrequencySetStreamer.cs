﻿using System.IO;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Test.Verification.Services
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