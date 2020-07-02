
using System;
using System.Runtime.Serialization;
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
            patientInfo.RegistryId = donorInfo.RegistryId;
            
            // Attempt to get the most specific sets first
            var donorSet = await repository.GetActiveSet(donorInfo.RegistryId, donorInfo.EthnicityId);
            var patientSet = await repository.GetActiveSet(patientInfo.RegistryId, patientInfo.EthnicityId);
            
            // If we didn't find ethnicity sets, find a generic one for that repository
            donorSet ??= await repository.GetActiveSet(donorInfo.RegistryId, null);
            patientSet ??= donorSet;
            
            // If no registry specific set exists, use a generic one.
            donorSet ??= await repository.GetActiveSet(null, null);
            patientSet ??= donorSet;

            if (donorSet == null || patientSet == null)
            {
                logger.SendTrace($"Did not find Haplotype Frequency Set for: \n Donor Registry: {donorInfo.RegistryId} Donor Ethnicity: {donorInfo.EthnicityId} \n Patient Registry: {patientInfo.RegistryId} Patient Ethnicity: {patientInfo.EthnicityId}", LogLevel.Error);
                throw new Exception("No Global Haplotype frequency set was found");
            }
            
            var result = new HaplotypeFrequencySetResponse
            {
                DonorSet = new HaplotypeFrequencySet(donorSet),
                PatientSet = new HaplotypeFrequencySet(patientSet)
            };
            return result;
        }
    }
}