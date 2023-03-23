using System.Diagnostics;
using Atlas.Common.ApplicationInsights;

namespace Atlas.Common.AzureStorage.ApplicationInsights
{
    public class AzureStorageEventModel : EventModel
    {
        private readonly Stopwatch azureStorageUploadTimer;

        public AzureStorageEventModel(string filename, string container)
            : base("Azure Storage")
        {
            azureStorageUploadTimer = new Stopwatch();
            Properties.Add("Filename", filename);
            Properties.Add("Container", container);
            Level = LogLevel.Verbose;
        }

        public AzureStorageEventModel(string filename)
            : base("Azure Storage")
        {
            azureStorageUploadTimer = new Stopwatch();
            Properties.Add("Filename", filename);
            Level = LogLevel.Verbose;
        }

        public void StartAzureStorageCommunication()
        {
            azureStorageUploadTimer.Start();
        }

        public void EndAzureStorageCommunication(string action)
        {
            azureStorageUploadTimer.Stop();
            Metrics.Add($"Azure Storage - {action} - duration /ms", azureStorageUploadTimer.ElapsedMilliseconds);
        }
    }
}
