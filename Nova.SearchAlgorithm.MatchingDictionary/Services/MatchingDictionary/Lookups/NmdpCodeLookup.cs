using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class NmdpCodeLookup : MatchingDictionaryLookup
    {
        private readonly IHlaServiceClient hlaServiceClient;

        public NmdpCodeLookup(IMatchingDictionaryRepository dictionaryRepository, IHlaServiceClient hlaServiceClient) : base(dictionaryRepository)
        {
            this.hlaServiceClient = hlaServiceClient;
        }

        public override async Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleles = await ExpandNmdpCode(matchLocus, lookupName);
            var tasks = alleles.Select(allele => GetMatchingDictionaryEntry(matchLocus, allele, TypingMethod.Molecular));
            var entries = await Task.WhenAll(tasks);

            return new MatchingDictionaryEntry(
                    matchLocus,
                    lookupName,
                    TypingMethod.Molecular,
                    MolecularSubtype.NmdpCode,
                    SerologySubtype.NotSerologyTyping,
                    entries.SelectMany(p => p.MatchingPGroups).Distinct(),
                    entries.SelectMany(g => g.MatchingGGroups).Distinct(),
                    entries.SelectMany(s => s.MatchingSerologies).Distinct()
                );
        }

        private async Task<IEnumerable<string>> ExpandNmdpCode(MatchLocus matchLocus, string lookupName)
        {
            Enum.TryParse(matchLocus.ToString(), true, out MolecularLocusType molLocusType);
            return await hlaServiceClient.GetAllelesForDefinedNmdpCode(molLocusType, lookupName);
        }
    }
}
