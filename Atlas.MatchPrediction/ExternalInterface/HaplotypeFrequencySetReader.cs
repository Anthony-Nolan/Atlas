using Atlas.MatchPrediction.Data.Repositories;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IHaplotypeFrequencySetReader
    {
        Task<HaplotypeFrequencySet> GetActiveGlobalHaplotypeFrequencySet();
        Task<IEnumerable<HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySetByPopulationId(int populationId);

        /// <summary>
        /// The set with the given id, active or not; null when there is none. For a caller that imputes against a set it
        /// chose, such as an older set that a newer import has replaced.
        /// </summary>
        Task<HaplotypeFrequencySet> GetHaplotypeFrequencySet(int id);
    }

    internal class HaplotypeFrequencySetReader : IHaplotypeFrequencySetReader
    {
        private readonly IHaplotypeFrequencySetReadRepository readRepository;

        public HaplotypeFrequencySetReader(IHaplotypeFrequencySetReadRepository readRepository)
        {
            this.readRepository = readRepository;
        }

        public async Task<HaplotypeFrequencySet> GetActiveGlobalHaplotypeFrequencySet()
        {
            var set = await readRepository.GetActiveHaplotypeFrequencySet(null, null);
            return ConvertToExternalModel(set);
        }

        public async Task<IEnumerable<HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySetByPopulationId(int populationId)
        {
            var sets = await readRepository.GetActiveHaplotypeFrequencySet(populationId);
            return sets.Select(ConvertToExternalModel);
        }

        /// <inheritdoc />
        public async Task<HaplotypeFrequencySet> GetHaplotypeFrequencySet(int id)
        {
            var set = await readRepository.GetHaplotypeFrequencySet(id);
            return set == null ? null : ConvertToExternalModel(set);
        }

        private static HaplotypeFrequencySet ConvertToExternalModel(Data.Models.HaplotypeFrequencySet set)
        {
            return new HaplotypeFrequencySet
            {
                Id = set.Id,
                Name = set.Name,
                RegistryCode = set.RegistryCode,
                EthnicityCode = set.EthnicityCode,
                PopulationId = set.PopulationId,
                HlaNomenclatureVersion = set.HlaNomenclatureVersion
            };
        }
    }
}