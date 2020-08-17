using Atlas.Common.GeneticData.Hla.Services;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Dasync.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface IMacExpander
    {
        /// <summary>
        /// Expands and stores latest generic codes not already in the database.
        /// </summary>
        /// <returns></returns>
        Task ExpandAndStoreLatestGenericMacs();
    }

    internal class MacExpander : IMacExpander
    {
        private const int BatchSize = 1000;
        private readonly IMacFetcher macFetcher;
        private readonly IExpandedMacsRepository repository;

        public MacExpander(IMacFetcher macFetcher, IExpandedMacsRepository repository)
        {
            this.macFetcher = macFetcher;
            this.repository = repository;
        }

        public async Task ExpandAndStoreLatestGenericMacs()
        {
            Debug.WriteLine("Fetching MACs for expansion.");

            var genericMacs = await FetchLatestGenericMacs();

            await genericMacs
                .SelectMany(Expand)
                .Batch(BatchSize)
                .ForEachAsync(async batch => await repository.BulkInsert(batch));

            Debug.WriteLine("MAC expansion completed.");
        }

        /// <summary>
        /// Retrieves latest generic MACs direct from the NMDP source.
        /// This is more performant than fetching large number of MACs from the MAC dictionary cloud table.
        /// </summary>
        private async Task<IAsyncEnumerable<Mac>> FetchLatestGenericMacs()
        {
            var lastCode = await GetLastStoredCode();

            // MACs are retrieved using a service that is internal to the MAC dictionary project.
            // The service was intentionally kept internal (and not exposed via the External Interface) to ensure
            // that production code only ever uses the dictionary as the single source of MAC lookups.
            return IAsyncEnumerableExtensions.Where(macFetcher.FetchAndLazilyParseMacsSince(lastCode), m => m.IsGeneric);
        }

        private async Task<string> GetLastStoredCode()
        {
            var lastCode = await repository.GetLastCodeInserted();

            // It's possible that rows for the last code may not have all been successfully inserted.
            // E.g., In case of service interruption. So it is safer to delete them before importing the latest codes.
            await repository.DeleteCode(lastCode);

            return await repository.GetLastCodeInserted();
        }

        private static IEnumerable<ExpandedMac> Expand(Mac mac)
        {
            Debug.WriteLine($"Expanding code: {mac.Code}.");

            return AlleleStringSplitter.SplitAlleleString(mac.Hla).Select(secondField => new ExpandedMac
            {
                SecondField = secondField,
                Code = mac.Code
            });
        }
    }
}