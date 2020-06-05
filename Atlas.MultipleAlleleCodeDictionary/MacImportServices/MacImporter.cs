using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Polly;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportServices
{
    public interface IMacImporter
    {
        public Task ImportLatestMultipleAlleleCodes();
    }

    public class MacImporter : IMacImporter
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

        public async Task ImportLatestMultipleAlleleCodes()
        {
            const string tracePrefix = "Mac Import: ";
            logger.SendTrace($"{tracePrefix}Mac Import started", LogLevel.Info);
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"{tracePrefix}The last MAC entry found was: {lastEntryBeforeInsert}", LogLevel.Info);

                List<MultipleAlleleCodeEntity> newMacs;
                await using (var macStream = await DownloadMacs())
                {
                    newMacs = await macParser.GetMacsSinceLastEntry(macStream, lastEntryBeforeInsert);
                }

                logger.SendTrace($"{tracePrefix}Attempting to insert {newMacs.Count} new MACs", LogLevel.Info);
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new ErrorEventModel($"{tracePrefix}Failed to finish MAC Import", e));
                throw;
            }

            logger.SendTrace($"{tracePrefix}Successfully finished MAC Import", LogLevel.Info);
        }

        private async Task<Stream> DownloadMacs()
        {
            var retryPolicy = Policy.Handle<Exception>().Retry(3);
            return await retryPolicy.Execute(async () => await macCodeDownloader.DownloadAndUnzipStream());
        }
    }
}