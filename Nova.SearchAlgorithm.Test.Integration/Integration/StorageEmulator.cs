using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;

namespace Nova.SearchAlgorithm.Test.Integration.Integration
{
    public class StorageEmulator
    {
        private readonly string StorageEmulatorLocation = ConfigurationManager.AppSettings["emulatorLocation"];

        private readonly Lazy<CloudTable> donorTable = new Lazy<CloudTable>(() => GetTable(CloudTableStorage.DonorTableReference));
        private readonly Lazy<CloudTable> tableRefTable = new Lazy<CloudTable>(() => GetTable(TableReferenceRepository.CloudTableReference));

        private bool wasRunning;

        public void Start()
        {
            wasRunning = IsRunning();
            if (!wasRunning)
            {
                ExecuteCommandOnEmulator("start");
            }
        }

        public void Stop()
        {
            if (!wasRunning)
            {
                ExecuteCommandOnEmulator("stop");
            }
        }

        private bool IsRunning()
        {
            var output = ExecuteCommandOnEmulator("status");
            return output.Contains("IsRunning: True");
        }

        public void Clear()
        {
            // Only clear donors and matches, to avoid developers needing to regenerate the matching dictionary.
            // (Unfortunately a dev machine can only run one emulated storage environment)
            Task.WhenAll(
                donorTable.Value.DeleteAsync(),
                tableRefTable.Value.DeleteAsync()
                ).Wait();
        }

        private string ExecuteCommandOnEmulator(string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = StorageEmulatorLocation,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            Process proc = new Process
            {
                StartInfo = start
            };

            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
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
