using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;

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
        private readonly IFrequencySetStreamer setStreamer;
        private readonly IFrequencyCsvReader csvReader;

        public HaplotypeFrequenciesReader(
            IHaplotypeFrequencySetReader setReader,
            IFrequencySetStreamer setStreamer,
            IFrequencyCsvReader csvReader)
        {
            this.setReader = setReader;
            this.setStreamer = setStreamer;
            this.csvReader = csvReader;
        }

        public async Task<HaplotypeFrequenciesReaderResult> GetUnalteredActiveGlobalHaplotypeFrequencies()
        {
            var set = await setReader.GetActiveGlobalHaplotypeFrequencySet();
            var frequencies = await ReadHaplotypeFrequenciesFromFile(set);

            return new HaplotypeFrequenciesReaderResult
            {
                HaplotypeFrequencySetId = set.Id,

                // TODO ATLAS-600 - Set version from value saved with HF set row in db
                HlaNomenclatureVersion = "3410",

                HaplotypeFrequencies = frequencies
            };
        }

        private async Task<IReadOnlyCollection<HaplotypeFrequency>> ReadHaplotypeFrequenciesFromFile(HaplotypeFrequencySet set)
        {
            var fileStream = await setStreamer.GetFileContents(set.Name);

            return csvReader
                .GetFrequencies(fileStream)
                .ToList();
        }
    }

    public class HaplotypeFrequenciesReaderResult
    {
        public int? HaplotypeFrequencySetId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public IReadOnlyCollection<HaplotypeFrequency> HaplotypeFrequencies { get; set; }
    }
}
