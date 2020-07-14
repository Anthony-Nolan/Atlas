using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour);

        /// <summary>
        /// Relies on the fact that G groups will have 1 or 0 corresponding P Groups.
        /// </summary>
        /// <returns>
        /// A dictionary of G Group to P Group.
        /// If a GGroup has no corresponding P Group, it will not be present.
        /// </returns>
        Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup, string hlaNomenclatureVersion);
    }

    internal class HlaConversionBehaviour
    {
        public TargetHlaCategory TargetHlaCategory { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }

    internal class HlaConverter : IHlaConverter
    {
        private readonly IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter;
        private readonly IHlaScoringMetadataService scoringMetadataService;
        private readonly ILogger logger;
        private readonly IAppCache cache;

        public HlaConverter(
            IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter,
            IHlaScoringMetadataService scoringMetadataService,
            ILogger logger,
            // ReSharper disable once SuggestBaseTypeForParameter
            IPersistentCacheProvider persistentCacheProvider)
        {
            this.hlaNameToTwoFieldAlleleConverter = hlaNameToTwoFieldAlleleConverter;
            this.scoringMetadataService = scoringMetadataService;
            this.logger = logger;
            cache = persistentCacheProvider.Cache;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, HlaConversionBehaviour conversionBehaviour)
        {
            if (hlaName.IsNullOrEmpty() || conversionBehaviour == null)
            {
                throw new ArgumentNullException();
            }

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (conversionBehaviour.TargetHlaCategory)
            {
                case TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(locus, hlaName, ExpressionSuffixBehaviour.Include);
                case TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix:
                    return await hlaNameToTwoFieldAlleleConverter.ConvertHla(locus, hlaName, ExpressionSuffixBehaviour.Exclude);
                case TargetHlaCategory.GGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate GGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingGGroups.ToList();
                case TargetHlaCategory.PGroup:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate PGroup lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion)).MatchingPGroups.ToList();
                case TargetHlaCategory.Serology:
                    //TODO ATLAS-394: After HMD has been decoupled from Scoring, use appropriate Serology lookup service
                    return (await GetHlaScoringInfo(locus, hlaName, conversionBehaviour.HlaNomenclatureVersion))
                        .MatchingSerologies.Select(serology => serology.Name).ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(conversionBehaviour), conversionBehaviour, null);
            }
        }

        /// <inheritdoc />
        public async Task<string> ConvertGGroupToPGroup(Locus locus, string gGroup, string hlaNomenclatureVersion)
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
                var pGroups = await ConvertHla(
                    locus,
                    gGroup,
                    new HlaConversionBehaviour {HlaNomenclatureVersion = hlaNomenclatureVersion, TargetHlaCategory = TargetHlaCategory.PGroup}
                );
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

        private async Task<IHlaScoringInfo> GetHlaScoringInfo(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            return (await scoringMetadataService.GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion)).HlaScoringInfo;
        }
    }
}