﻿namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public string Name { get; set; }
        public int PopulationId { get; set; }

        public Client.Models.Search.Results.MatchPrediction.HaplotypeFrequencySet ToClientHaplotypeFrequencySet()
        {
            return new Client.Models.Search.Results.MatchPrediction.HaplotypeFrequencySet
            {
                Id = Id,
                RegistryCode = RegistryCode,
                EthnicityCode = EthnicityCode,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                PopulationId = PopulationId
            };
        }
    }
}