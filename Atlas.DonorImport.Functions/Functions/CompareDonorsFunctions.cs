using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorComparer;
using Atlas.DonorImport.Services.DonorIdChecker;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Atlas.DonorImport.Functions.Functions
{
    public class CompareDonorsFunctions
    {
        private readonly IDonorComparer donorComparer;

        public CompareDonorsFunctions(IDonorComparer donorComparer)
        {
            this.donorComparer = donorComparer;
        }


        [FunctionName(nameof(CompareDonorsFromFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task CompareDonorsFromFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:CompareDonorsTopic%",
                "%MessagingServiceBus:CompareDonorsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent, string messageId,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(new DonorImportFile()
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject
            });
        }
    }
}
