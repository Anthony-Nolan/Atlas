using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaNameToPGroupConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, string hlaNomenclatureVersion);

        /// <summary>
        /// This is an optimised code path specifically for G-Group to P-Group conversion,
        /// reliant on the fact that each G Group will only have 1 or 0 corresponding P Groups.
        ///
        /// Equivalent to calling <see cref="ConvertHla"/> and then calling `SingleOrDefault`, but faster. 
        /// </summary>
        Task<string> ConvertGGroup(Locus locus, string gGroup, string hlaNomenclatureVersion);
    }

    internal class HlaNameToPGroupConverter : IHlaNameToPGroupConverter
    {
        private readonly IAppCache cache;
        private readonly IHlaScoringMetadataService scoringMetadataService;
        private readonly ILogger logger;

        public HlaNameToPGroupConverter(
            IHlaScoringMetadataService scoringMetadataService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IPersistentCacheProvider persistentCacheProvider,
            ILogger logger)
        {
            cache = persistentCacheProvider.Cache;
            this.scoringMetadataService = scoringMetadataService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate PGroup lookup service
            return (await scoringMetadataService.GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion)).HlaScoringInfo.MatchingPGroups.ToList();
        }

        /// <inheritdoc />
        public async Task<string> ConvertGGroup(Locus locus, string gGroup, string hlaNomenclatureVersion)
        {
            if (gGroup == null)
            {
                return null;
            }

            var dictionary = await GetGGroupToPGroupDictionary(locus, hlaNomenclatureVersion);

            // Null is an appropriate value in some cases - G Groups corresponding to a null allele will have no P Group. 
            return dictionary.GetValueOrDefault(gGroup);
        }

        private async Task<IReadOnlyDictionary<string, string>> GetGGroupToPGroupDictionary(Locus locus, string hlaNomenclatureVersion)
        {
            var cacheKey = $"{locus}-GToPGroupLookup-{hlaNomenclatureVersion}";
            return await cache.GetOrAddAsync(cacheKey, async _ =>
            {
                return await logger.RunTimedAsync(async () =>
                    {
                        var perLocusGGroups = (await scoringMetadataService.GetAllGGroups(hlaNomenclatureVersion))[locus];
                        return await BuildPerLocusGGroupToPGroupDictionary(locus, perLocusGGroups, hlaNomenclatureVersion);
                    },
                    $"Calculated GGroup to PGroup lookup for locus {locus}");
            });
        }

        private async Task<Dictionary<string, string>> BuildPerLocusGGroupToPGroupDictionary(
            Locus locus,
            IEnumerable<string> gGroups,
            string hlaNomenclatureVersion)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var gGroup in gGroups)
            {
                var pGroups = await ConvertHla(locus, gGroup, hlaNomenclatureVersion);
                if (pGroups.Count > 1)
                {
                    const string errorMessage = "Encountered G Group with multiple corresponding P Groups. This is not expected to be possible.";
                    logger.SendTrace(errorMessage, LogLevel.Error, new Dictionary<string, string> {{"GGroup", gGroup}});
                    throw new Exception(errorMessage);
                }

                if (pGroups.Count == 1)
                {
                    dictionary[gGroup] = pGroups.Single();
                }
            }

            return dictionary;
        }
    }
}