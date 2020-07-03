
using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencySets
{
    public interface IHaplotypeFrequencySetService
    {
        Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(IndividualPopulationData donorInfo, IndividualPopulationData patientInfo);
    }
    
    internal class HaplotypeFrequencySetService : IHaplotypeFrequencySetService
    {
        private readonly IHaplotypeFrequencySetRepository repository;
        private readonly ILogger logger; 

        public HaplotypeFrequencySetService(IHaplotypeFrequencySetRepository repository, ILogger logger)
        {
            this.repository = repository;
            this.logger = logger;
        }
        
        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(IndividualPopulationData donorInfo, IndividualPopulationData patientInfo)
        {
            // Patients should use the donors registry.
            patientInfo.RegistryCode ??= donorInfo.RegistryCode;
            
            // Attempt to get the most specific sets first
            var donorSet = await repository.GetActiveSet(donorInfo.RegistryCode, donorInfo.EthnicityCode);
            var patientSet = await repository.GetActiveSet(patientInfo.RegistryCode, patientInfo.EthnicityCode);
            
            // If we didn't find ethnicity sets, find a generic one for that repository
            donorSet ??= await repository.GetActiveSet(donorInfo.RegistryCode, null);
            patientSet ??= donorSet;
            
            // If no registry specific set exists, use a generic one.
            donorSet ??= await repository.GetActiveSet(null, null);
            patientSet ??= donorSet;

            if (donorSet == null || patientSet == null)
            {
                logger.SendTrace($"Did not find Haplotype Frequency Set for: \n Donor Registry: {donorInfo.RegistryCode} Donor Ethnicity: {donorInfo.EthnicityCode} \n Patient Registry: {patientInfo.RegistryCode} Patient Ethnicity: {patientInfo.EthnicityCode}", LogLevel.Error);
                throw new Exception("No Global Haplotype frequency set was found");
            }
            
            return new HaplotypeFrequencySetResponse
            {
                DonorSet = MapDataModelToClientModel(donorSet),
                PatientSet = MapDataModelToClientModel(patientSet)
            };
        }

        private HaplotypeFrequencySet MapDataModelToClientModel(Data.Models.HaplotypeFrequencySet set)
        {
            return new HaplotypeFrequencySet
            {
                EthnicityCode = set.EthnicityCode,
                Id = set.Id,
                Name = set.Name,
                RegistryCode = set.RegistryCode
            };
        }
    }
}