using System;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencySets
{
    internal interface IHaplotypeFrequencySetService
    {
        Task<HaplotypeFrequencySet> GetHaplotypeFrequencySetId(IndividualMetaData donorInfo, IndividualMetaData patientInfo);
    }
    
    internal class HaplotypeFrequencySetService : IHaplotypeFrequencySetService
    {
        private readonly IHaplotypeFrequencySetRepository repository;

        public HaplotypeFrequencySetService(IHaplotypeFrequencySetRepository repository)
        {
            this.repository = repository;
        }
        
        public async Task<HaplotypeFrequencySet> GetHaplotypeFrequencySetId(IndividualMetaData donorInfo, IndividualMetaData patientInfo)
        {
            return await repository.GetActiveSet(donorInfo.RegistryId, donorInfo.EthnicityId);
        }
    }
}