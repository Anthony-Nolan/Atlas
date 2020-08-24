using Atlas.MatchPrediction.Data.Repositories;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IHaplotypeFrequencySetReader
    {
        Task<HaplotypeFrequencySet> GetActiveGlobalHaplotypeFrequencySet();
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

            return new HaplotypeFrequencySet
            {
                Id = set.Id,
                Name = set.Name,
                RegistryCode = set.RegistryCode,
                EthnicityCode = set.EthnicityCode,
                HlaNomenclatureVersion = set.HlaNomenclatureVersion
            };
        }
    }
}
