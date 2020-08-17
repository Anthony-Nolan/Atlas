using Atlas.Common.GeneticData.Hla.Services;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
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
        private readonly IMacStreamer macStreamer;
        private readonly IExpandedMacsRepository repository;

        public MacExpander(IMacStreamer macStreamer, IExpandedMacsRepository repository)
        {
            this.macStreamer = macStreamer;
            this.repository = repository;
        }

        public async Task ExpandAndStoreLatestGenericMacs()
        {
            var genericMacs = await StreamLatestGenericMacs();

            var macCounter = 0;
            var expanded = new List<ExpandedMac>();
            await foreach (var mac in genericMacs)
            {
                Debug.WriteLine($"Expanding code: {mac.Code}.");

                expanded.AddRange(Expand(mac));
                macCounter++;

                if (macCounter == BatchSize)
                {
                    await repository.BulkInsert(expanded);
                    expanded = new List<ExpandedMac>();
                    macCounter = 0;
                }
            }

            if (expanded.Any())
            {
                await repository.BulkInsert(expanded);
            }
        }

        private async Task<IAsyncEnumerable<Mac>> StreamLatestGenericMacs()
        {
            var lastCode = await GetLastStoredCode();
            return (await macStreamer.StreamMacsSince(lastCode)).Where(m => m.IsGeneric);
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
            return AlleleStringSplitter.SplitAlleleString(mac.Hla).Select(secondField => new ExpandedMac
            {
                SecondField = secondField,
                Code = mac.Code
            });
        }
    }
}
