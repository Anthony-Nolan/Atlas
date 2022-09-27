using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation
{
    internal interface IHaplotypeFrequenciesReader
    {
        /// <summary>
        /// Haplotype frequencies are manipulated before being persisted to the db, in order to optimise MPA performance.
        /// This method retrieves the original, unaltered haplotype frequencies, used to generate the currently active, global HF set.
        /// </summary>
        Task<HaplotypeFrequenciesReaderResult> GetUnalteredActiveGlobalHaplotypeFrequencies();
    }

    internal class HaplotypeFrequenciesReader : IHaplotypeFrequenciesReader
    {
        private readonly IHaplotypeFrequencySetReader setReader;
        private readonly IBlobStreamer setStreamer;
        private readonly IFrequencyFileParser fileParser;
        private readonly string containerName;

        public HaplotypeFrequenciesReader(
            IHaplotypeFrequencySetReader setReader,
            IBlobStreamer setStreamer,
            IFrequencyFileParser fileParser,
            IOptions<VerificationAzureStorageSettings> settings)
        {
            this.setReader = setReader;
            this.setStreamer = setStreamer;
            this.fileParser = fileParser;
            this.containerName = settings.Value.HaplotypeFrequencySetBlobContainer;
        }

        public async Task<HaplotypeFrequenciesReaderResult> GetUnalteredActiveGlobalHaplotypeFrequencies()
        {
            var set = await setReader.GetActiveGlobalHaplotypeFrequencySet();
            var frequencies = await ReadHaplotypeFrequenciesFromFile(set);

            if (frequencies.TypingCategory == null)
            {
                throw new Exception("Could not determine the typing category of the haplotype frequency set.");
            }

            return new HaplotypeFrequenciesReaderResult
            {
                HaplotypeFrequencySetId = set.Id,
                HlaNomenclatureVersion = frequencies.HlaNomenclatureVersion,
                TypingCategory = frequencies.TypingCategory.Value,
                HaplotypeFrequencies = frequencies.Frequencies
            };
        }

        private async Task<FrequencySetFileSchema> ReadHaplotypeFrequenciesFromFile(HaplotypeFrequencySet set)
        {
            var fileStream = await setStreamer.GetBlobContents(containerName, set.Name);
            return fileParser.GetFrequencies(fileStream);
        }
    }

    public class HaplotypeFrequenciesReaderResult
    {
        public int? HaplotypeFrequencySetId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public ImportTypingCategory TypingCategory { get; set; }
        public IEnumerable<FrequencyRecord> HaplotypeFrequencies { get; set; }
    }
}
