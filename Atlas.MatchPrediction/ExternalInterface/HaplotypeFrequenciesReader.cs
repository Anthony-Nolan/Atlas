using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HaplotypeFrequency = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequency;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IHaplotypeFrequenciesReader
    {
        /// <summary>
        /// Haplotype frequencies are manipulated before being persisted to the db, in order to optimise MPA performance.
        /// This method retrieves the original, unaltered haplotype frequencies, used to generate the currently active, global HF set.
        /// </summary>
        Task<HaplotypeFrequenciesReaderResult> GetUnalteredActiveGlobalHaplotypeFrequencies();
    }

    internal class HaplotypeFrequenciesReader : IHaplotypeFrequenciesReader
    {
        private readonly IHaplotypeFrequenciesReadRepository readRepository;
        private readonly IFrequencySetStreamer setStreamer;
        private readonly IFrequencyCsvReader csvReader;

        public HaplotypeFrequenciesReader(
            IHaplotypeFrequenciesReadRepository readRepository,
            IFrequencySetStreamer setStreamer,
            IFrequencyCsvReader csvReader)
        {
            this.readRepository = readRepository;
            this.setStreamer = setStreamer;
            this.csvReader = csvReader;
        }

        public async Task<HaplotypeFrequenciesReaderResult> GetUnalteredActiveGlobalHaplotypeFrequencies()
        {
            var set = await readRepository.GetActiveHaplotypeFrequencySet(null, null);
            var frequencies = await ReadHaplotypeFrequenciesFromFile(set);

            return new HaplotypeFrequenciesReaderResult
            {
                HaplotypeFrequencySetId = set.Id,
                HaplotypeFrequencies = frequencies
            };
        }

        private async Task<IReadOnlyCollection<HaplotypeFrequency>> ReadHaplotypeFrequenciesFromFile(HaplotypeFrequencySet set)
        {
            var fileStream = await setStreamer.GetFileContents(set.Name);

            return csvReader
                .GetFrequencies(fileStream)
                .Select(MapFromDataModelToExternalModel)
                .ToList();
        }

        private static HaplotypeFrequency MapFromDataModelToExternalModel(Data.Models.HaplotypeFrequency frequency)
        {
            return new HaplotypeFrequency
            {
                Frequency = frequency.Frequency,
                A = frequency.A,
                B = frequency.B,
                C = frequency.C,
                Dqb1 = frequency.DQB1,
                Drb1 = frequency.DRB1
            };
        }
    }

    public class HaplotypeFrequenciesReaderResult
    {
        public int? HaplotypeFrequencySetId { get; set; }
        public IReadOnlyCollection<HaplotypeFrequency> HaplotypeFrequencies { get; set; }
    }
}
