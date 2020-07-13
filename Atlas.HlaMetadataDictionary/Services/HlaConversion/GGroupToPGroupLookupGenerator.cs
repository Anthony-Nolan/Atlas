using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IGGroupToPGroupDictionaryGenerator
    {
        /// <summary>
        /// Relies on the fact that G groups will have 1 or 0 corresponding P Groups.
        /// </summary>
        /// <returns>
        /// A dictionary of G Group to P Group.
        /// If a GGroup has no corresponding P Group, it will not be present.
        /// </returns>
        Task<IReadOnlyDictionary<string, string>> GetGGroupToPGroupDictionary(
            Locus locus,
            Func<Task<IDictionary<Locus, List<string>>>> getAllGGroups,
            Func<Locus, string, Task<IReadOnlyCollection<string>>> lookupCorrespondingPGroups,
            string hlaNomenclatureVersion);
    }

    internal class GGroupToPGroupLookupGenerator : IGGroupToPGroupDictionaryGenerator
    {
        private readonly ILogger logger;
        private readonly IAppCache cache;

        // ReSharper disable once SuggestBaseTypeForParameter
        public GGroupToPGroupLookupGenerator(IPersistentCacheProvider persistentCacheProvider, ILogger logger)
        {
            this.logger = logger;
            cache = persistentCacheProvider.Cache;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, string>> GetGGroupToPGroupDictionary(
            Locus locus,
            Func<Task<IDictionary<Locus, List<string>>>> getAllGGroups,
            Func<Locus, string, Task<IReadOnlyCollection<string>>> lookupCorrespondingPGroups,
            string hlaNomenclatureVersion)
        {
            return await cache.GetOrAddAsync($"{locus}-GToPGroupLookup-{hlaNomenclatureVersion}", async _ =>
            {
                return await logger.RunTimedAsync(async () =>
                    {
                        var perLocusGGroups = (await getAllGGroups())[locus];
                        return await BuildPerLocusDictionary(locus, lookupCorrespondingPGroups, perLocusGGroups);
                    },
                    $"Calculated GGroup to PGroup lookup for locus {locus}");
            });
        }

        private async Task<Dictionary<string, string>> BuildPerLocusDictionary(
            Locus locus,
            Func<Locus, string, Task<IReadOnlyCollection<string>>> lookupCorrespondingPGroups,
            IEnumerable<string> perLocusGGroups)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var gGroup in perLocusGGroups)
            {
                var pGroups = await lookupCorrespondingPGroups(locus, gGroup);
                if (pGroups.Count > 1)
                {
                    const string errorMessage = "Encountered G Group with multiple corresponding P Groups. This is not expected to be possible.";
                    logger.SendTrace(errorMessage, LogLevel.Error, new Dictionary<string, string>{{"GGroup", gGroup}});
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