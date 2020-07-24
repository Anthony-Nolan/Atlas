using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.ExternalInterface
{
    /// <summary>
    /// Reader to allow consumers read-only access to stored haplotype frequencies.
    /// </summary>
    public interface IHaplotypeFrequenciesReader
    {
        Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveGlobalHaplotypeFrequencies();
    }

    internal class HaplotypeFrequenciesReader : IHaplotypeFrequenciesReader
    {
        private readonly IHaplotypeFrequenciesReadRepository repository;

        public HaplotypeFrequenciesReader(IHaplotypeFrequenciesReadRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveGlobalHaplotypeFrequencies()
        {
            return (await repository.GetActiveHaplotypeFrequencies(null, null))
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
}
