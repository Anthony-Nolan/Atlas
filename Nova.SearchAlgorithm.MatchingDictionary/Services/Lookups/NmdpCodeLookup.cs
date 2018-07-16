using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class NmdpCodeLookup : AlleleNamesLookupBase
    {
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IMemoryCache memoryCache;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly ILogger logger;

        public NmdpCodeLookup(
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IMemoryCache memoryCache,
            IHlaServiceClient hlaServiceClient,
            IAlleleStringSplitterService alleleSplitter,
            ILogger logger)
            : base(hlaMatchingLookupRepository, alleleNamesLookupService)
        {
            this.hlaServiceClient = hlaServiceClient;
            this.memoryCache = memoryCache;
            this.alleleSplitter = alleleSplitter;
            this.logger = logger;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(MatchLocus matchLocus, string lookupName)
        {
            if (memoryCache.TryGetValue($"Antigens_{matchLocus}", out Dictionary<string, string> antigenDictionary)
                && antigenDictionary.TryGetValue("*" + lookupName, out var alleleString))
            {
                return alleleSplitter.GetAlleleNamesFromAlleleString(alleleString);
            }

            logger.SendTrace("Failed to lookup nmdp code from cache", LogLevel.Info, new Dictionary<string, string>
            {
                {"MatchLocus", matchLocus.ToString()},
                {"LookupName", lookupName}
            });

            Enum.TryParse(matchLocus.ToString(), true, out MolecularLocusType molecularLocusType);
            return await hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocusType, lookupName);
        }
    }
}