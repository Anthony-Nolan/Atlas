using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Caching;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class NmdpCodeLookup : AlleleNamesLookupBase
    {
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IAntigenCache antigenCache;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly ILogger logger;

        public NmdpCodeLookup(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAntigenCache antigenCache,
            IHlaServiceClient hlaServiceClient,
            IAlleleStringSplitterService alleleSplitter,
            ILogger logger)
            : base(hlaLookupRepository, alleleNamesLookupService)
        {
            this.hlaServiceClient = hlaServiceClient;
            this.antigenCache = antigenCache;
            this.alleleSplitter = alleleSplitter;
            this.logger = logger;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            var antigenDictionary = await antigenCache.GetNmdpCodeLookup(locus);
            
            if (antigenDictionary != null && antigenDictionary.TryGetValue("*" + lookupName, out var alleleString))
            {
                return alleleSplitter.GetAlleleNamesFromAlleleString(alleleString);
            }

            logger.SendTrace("Failed to lookup nmdp code from cache", LogLevel.Warn, new Dictionary<string, string>
            {
                {"LocusName", locus.ToString()},
                {"LookupName", lookupName}
            });

            Enum.TryParse(locus.ToString(), true, out MolecularLocusType molecularLocusType);
            return await hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocusType, lookupName);
        }
    }
}