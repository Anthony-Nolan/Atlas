using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    internal class NmdpCodeLookup : MatchingDictionaryLookup
    {
        private readonly IHlaServiceClient hlaServiceClient;

        public NmdpCodeLookup(IMatchedHlaRepository dictionaryRepository, IHlaServiceClient hlaServiceClient) : base(dictionaryRepository)
        {
            this.hlaServiceClient = hlaServiceClient;
        }

        public override async Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var alleles = await ExpandNmdpCode(matchLocus, lookupName);
            var tasks = alleles.Select(allele => GetDictionaryEntry(matchLocus, allele, TypingMethod.Molecular)).ToArray();
            var entries = await Task.WhenAll(tasks);

            return new MatchingDictionaryEntry(
                    matchLocus,
                    lookupName,
                    TypingMethod.Molecular,
                    MolecularSubtype.NmdpCode,
                    SerologySubtype.NotSerologyTyping,
                    entries.SelectMany(p => p.MatchingPGroups).Distinct(),
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
