using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class StorageEmulator
    {
        private readonly string StorageEmulatorLocation = ConfigurationManager.AppSettings["emulatorLocation"];

        private readonly Lazy<CloudTable> donorTable = new Lazy<CloudTable>(() => GetTable(CloudTableStorage.DonorTableReference));
        private readonly Lazy<CloudTable> matchTable = new Lazy<CloudTable>(() => GetTable(CloudTableStorage.MatchTableReference));

        public void Start()
        {
            ExecuteCommandOnEmulator("start");
        }

        public void Stop()
        {
            ExecuteCommandOnEmulator("stop");
        }

        public void Clear()
        {
            // Only clear donors and matches, to avoid developers needing to regenerate the matching dictionary.
            // (Unfortunately a dev machine can only run one emulated storage environment)
            Task.WhenAll(
                donorTable.Value.DeleteAsync(),
                matchTable.Value.DeleteAsync()
                ).Wait();
        }

        private void ExecuteCommandOnEmulator(string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = StorageEmulatorLocation
            };
            Process proc = new Process
            {
                StartInfo = start
            };

            proc.Start();
            proc.WaitForExit();
        }

        private static CloudTable GetTable(string tableReferenceString)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(tableReferenceString);
            tableReference.CreateIfNotExists();
            return new CloudTable(tableReference.StorageUri, tableClient.Credentials);
        }
    }
}
