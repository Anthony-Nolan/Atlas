using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    /// <summary>
    /// Prepares the state of various Atlas features so that verification search requests can be run.
    /// </summary>
    public interface IAtlasPreparer
    {
        Task PrepareAtlasDonorStores(int testHarnessId);
        Task UpdateLatestExportRecord(CompletedDataRefresh dataRefresh);
    }

    internal class AtlasPreparer : IAtlasPreparer
    {
        private static readonly HttpClient HttpRequestClient = new HttpClient();

        private readonly ITestDonorExporter testDonorExporter;
        private readonly string dataRefreshRequestUrl;
        private readonly ITestDonorExportRepository exportRepository;
        private readonly ITestHarnessRepository testHarnessRepository;

        public AtlasPreparer(
            ITestDonorExporter testDonorExporter, 
            ITestDonorExportRepository exportRepository,
            ITestHarnessRepository testHarnessRepository,
            IOptions<VerificationDataRefreshSettings> settings)
        {
            this.testDonorExporter = testDonorExporter;
            this.exportRepository = exportRepository;
            this.testHarnessRepository = testHarnessRepository;
            dataRefreshRequestUrl = settings.Value.RequestUrl;
        }

        public async Task PrepareAtlasDonorStores(int testHarnessId)
        {
            if (await AnyIncompleteExports())
            {
                throw new Exception("Incomplete test donor export record(s) found; this suggests a previous export is still running " +
                                    "or that something went wrong with data refresh. See README for further guidance.");
            }

            if (await TestHarnessNotCompleted(testHarnessId))
            {
                throw new ArgumentException($"Cannot export donors for test harness {testHarnessId} as it is marked as incomplete.");
            }

            await ExportDonors(testHarnessId);
            await InvokeDataRefresh();
        }

        public async Task UpdateLatestExportRecord(CompletedDataRefresh dataRefresh)
        {
            await exportRepository.UpdateLatestRecordWithDataRefreshDetails(dataRefresh);

            var successStatus = dataRefresh.WasSuccessful ? "" : "not ";
            Debug.WriteLine($"Latest export record updated: data refresh {dataRefresh.DataRefreshRecordId} was {successStatus}successful.");
        }

        private async Task<bool> AnyIncompleteExports()
        {
            var incompleteRecords = await exportRepository.GetRecordsWithoutDataRefreshDetails();
            return incompleteRecords.Any();
        }

        private async Task<bool> TestHarnessNotCompleted(int testHarnessId)
        {
            return !await testHarnessRepository.WasTestHarnessCompleted(testHarnessId);
        }
        
        private async Task ExportDonors(int testHarnessId)
        {
            var recordId = await exportRepository.AddRecord(testHarnessId);
            await testDonorExporter.ExportDonorsToDonorStore(testHarnessId);
            await exportRepository.SetExportedDateTime(recordId);
        }

        private async Task InvokeDataRefresh()
        {
            Debug.WriteLine("Requesting data refresh on matching algorithm.");

            // need sufficient time for request to return - this is not equivalent to how long data refresh takes to complete.
            HttpRequestClient.Timeout = TimeSpan.FromMinutes(5);

            var response = await HttpRequestClient.PostAsync(dataRefreshRequestUrl, new StringContent("{}"));

            // do not throw on response failure as data refresh requests often timeout even if the job itself completes successfully
            var debugMessage = response.IsSuccessStatusCode
                ? "Data refresh request submitted - check relevant AI logs for detailed progress messages."
                : $"Data refresh request failed: {response.StatusCode} - {response.ReasonPhrase}";

            Debug.WriteLine(debugMessage);
        }
    }
}
