using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;
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
        private readonly INotificationsClient notificationsClient;

        public DonorFileImporter(
            IDonorImportFileParser fileParser,
            IDonorRecordChangeApplier donorRecordChangeApplier,
            INotificationsClient notificationsClient)
        {
            this.fileParser = fileParser;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
            this.notificationsClient = notificationsClient;
        }

        public async Task ImportDonorFile(DonorImportFile file)
        {
            try
            {
                var donorUpdates = fileParser.LazilyParseDonorUpdates(file.Contents);
                foreach (var donorUpdateBatch in donorUpdates.Batch(BatchSize))
                {
                    await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(donorUpdateBatch.ToList());
                }
            }
            catch (Exception e)
            {
                var summary = $"Donor Import Failed: {file.FileName}";
                var description = @$"Importing donors for file: {file.FileName} has failed. With exception {e.Message}. If there were more than 
                                  {BatchSize} donor updates in the file, the file may have been partially imported - manual investigation is 
                                  recommended. See Application Insights for more information.";
                var alert = new Alert(summary, description, Priority.Medium);

                await notificationsClient.SendAlert(alert);

                throw;
            }
        }
    }
}