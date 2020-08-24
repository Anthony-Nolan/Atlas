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

                // All frequencies in a set use the same nomenclature version, so we pick an arbitrary value
                HlaNomenclatureVersion = frequencies.FirstOrDefault()?.HlaNomenclatureVersion,

                HaplotypeFrequencies = frequencies
            };
        }

        private async Task<IReadOnlyCollection<HaplotypeFrequencyMetadata>> ReadHaplotypeFrequenciesFromFile(HaplotypeFrequencySet set)
        {
            var fileStream = await setStreamer.GetFileContents(set.Name);

            return csvReader
                .ImportHaplotypeFrequencyRecord(fileStream)
                .ToList();
        }
    }

    public class HaplotypeFrequenciesReaderResult
    {
        public int? HaplotypeFrequencySetId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public IReadOnlyCollection<HaplotypeFrequencyMetadata> HaplotypeFrequencies { get; set; }
    }
}
