using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class NmdpCodeLookup : MultipleAllelesLookup
    {
        private readonly IHlaServiceClient hlaServiceClient;

        public NmdpCodeLookup(IMatchingDictionaryRepository dictionaryRepository, IHlaServiceClient hlaServiceClient) : base(dictionaryRepository)
        {
            this.hlaServiceClient = hlaServiceClient;
        }

        protected override async Task<IEnumerable<string>> GetAlleles(MatchLocus matchLocus, string lookupName)
        {
            Enum.TryParse(matchLocus.ToString(), true, out MolecularLocusType molLocusType);
            return await hlaServiceClient.GetAllelesForDefinedNmdpCode(molLocusType, lookupName);
        }
    }
}
