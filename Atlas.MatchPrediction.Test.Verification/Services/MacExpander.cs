using System.Collections.Generic;
using System.Diagnostics;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface IMacExpander
    {
        /// <summary>
        /// Expands and stores latest generic codes not already in the database.
        /// </summary>
        /// <returns></returns>
        Task ExpandLatestGenericMacs();
    }

    internal class MacExpander : IMacExpander
    {
        private readonly IMacStreamer macStreamer;
        private readonly IExpandedMacsRepository repository;

        public MacExpander(IMacStreamer macStreamer, IExpandedMacsRepository repository)
        {
            this.macStreamer = macStreamer;
            this.repository = repository;
        }

        public async Task ExpandLatestGenericMacs()
        {
            var macs = await DownloadLatestMacsAsync();

            const int bulkInsertPoint = 1000;
            var macCounter = 0;
            var expanded = new List<ExpandedMac>();
            await foreach (var mac in macs)
            {
                if (!mac.IsGeneric)
                {
                    continue;
                }

                macCounter++;

                Debug.WriteLine($"Expanding code: {mac.Code}.");

                expanded.AddRange(mac.SplitHla.Select(secondField => new ExpandedMac
                {
                    SecondField = secondField,
                    Code = mac.Code
                }));

                if (macCounter == bulkInsertPoint)
                {
                    await repository.BulkInsert(expanded);
                    macCounter = 0;
                    expanded = new List<ExpandedMac>();
                }
            }

            if (expanded.Any())
            {
                await repository.BulkInsert(expanded);
            }
        }

        private async Task<IAsyncEnumerable<Mac>> DownloadLatestMacsAsync()
        {
            // this block handles possibility that rows for the last code may not
            // have all been successfully inserted, e.g., in case of service interruption
            var lastCode = await repository.GetLastCodeInserted();
            await repository.DeleteCode(lastCode);
            var newLastCode = await repository.GetLastCodeInserted();

            return await macStreamer.StreamLatestMacsAsync(newLastCode);
        }
    }
}
