using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface.Models;
using MoreLinq.Extensions;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(DonorImportFile file);
    }

    internal class DonorFileImporter : IDonorFileImporter
    {
        private const int BatchSize = 10000;
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorRecordChangeApplier donorRecordChangeApplier;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;

        public DonorFileImporter(
            IDonorImportFileParser fileParser,
            IDonorRecordChangeApplier donorRecordChangeApplier,
            INotificationSender notificationSender,
            ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task ImportDonorFile(DonorImportFile file)
        {
            var importedDonorCount = 0;
            try
            {
                var donorUpdates = fileParser.LazilyParseDonorUpdates(file.Contents);
                foreach (var donorUpdateBatch in donorUpdates.Batch(BatchSize))
                {
                    var reifiedDonorBatch = donorUpdateBatch.ToList();
                    await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(reifiedDonorBatch, file.FileLocation);
                    importedDonorCount += reifiedDonorBatch.Count;
                }
                logger.SendTrace($"Donor Import for file '{file.FileLocation}' complete. Imported {importedDonorCount} donor(s).", LogLevel.Info);
            }
            catch (Exception e)
            {
                var summary = $"Donor Import Failed: {file.FileLocation}";
                var description = @$"Importing donors for file: {file.FileLocation} has failed. With exception {e.Message}.
{importedDonorCount} Donors were successfully imported prior to this error and have already been stored in the Database. Any remaining donors in the file have not been stored.
Manual investigation is recommended; see Application Insights for more information.";

                await notificationSender.SendAlert(summary, description, Priority.Medium, nameof(ImportDonorFile));

                throw;
            }
        }
    }
}