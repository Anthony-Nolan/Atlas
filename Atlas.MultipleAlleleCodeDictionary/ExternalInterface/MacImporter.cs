using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacImporter
    {
        public Task ImportLatestMacs();
        public Task RecreateMacTable();
    }

    internal class MacImporter : IMacImporter
    {
        private readonly IMacRepository macRepository;
        private readonly IMacParser macParser;
        private readonly ILogger logger;
        private readonly IMacCodeDownloader macCodeDownloader;

        public MacImporter(IMacRepository macRepository, IMacParser macParser, ILogger logger, IMacCodeDownloader macCodeDownloader)
        {
            this.macRepository = macRepository;
            this.macParser = macParser;
            this.logger = logger;
            this.macCodeDownloader = macCodeDownloader;
        }

        public async Task RecreateMacTable()
        {
            await macRepository.TruncateMacTable();
            await ImportLatestMacs();
        }

        public async Task ImportLatestMacs()
        {
            const string tracePrefix = "Mac Import: ";
            logger.SendTrace($"{tracePrefix}Mac Import started");
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"{tracePrefix}The last MAC entry found was: {lastEntryBeforeInsert}");

                List<Mac> newMacs;
                await using (var macStream = await DownloadMacs())
                {
                    newMacs = await macParser.GetMacsSince(macStream, lastEntryBeforeInsert);
                }

                logger.SendTrace($"{tracePrefix}Attempting to insert {newMacs.Count} new MACs");
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new ErrorEventModel($"{tracePrefix}Failed to finish MAC Import", e));
                throw;
            }

            logger.SendTrace($"{tracePrefix}Successfully finished MAC Import");
        }

        private async Task<Stream> DownloadMacs()
        {
            var retryPolicy = Policy.Handle<Exception>().Retry(3);
            return await retryPolicy.Execute(async () => await macCodeDownloader.DownloadAndUnzipStream());
        }
    }
}