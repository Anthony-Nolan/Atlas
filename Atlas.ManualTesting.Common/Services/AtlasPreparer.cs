using Atlas.DonorImport.Data.Models;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Atlas.ManualTesting.Common.Services
{
    /// <summary>
    /// Prepares the state of various Atlas features so that search requests can be run.
    /// </summary>
    public interface IAtlasPreparer
    {
        Task SaveDataRefreshDetails(CompletedDataRefresh dataRefresh);
    }

    public abstract class AtlasPreparer : IAtlasPreparer
    {
        private readonly ITestDonorExporter testDonorExporter;
        private readonly ITestDonorExportRepository exportRepository;
        private readonly string dataRefreshRequestUrl;

        protected AtlasPreparer(
            ITestDonorExporter testDonorExporter,
            ITestDonorExportRepository exportRepository,
            string dataRefreshRequestUrl)
        {
            this.testDonorExporter = testDonorExporter;
            this.exportRepository = exportRepository;
            this.dataRefreshRequestUrl = dataRefreshRequestUrl;
        }

        public async Task SaveDataRefreshDetails(CompletedDataRefresh dataRefresh)
        {
            await exportRepository.SetCompletedDataRefreshInfo(dataRefresh);

            var successStatus = dataRefresh.WasSuccessful ? "" : "not ";
            Debug.WriteLine($"Latest export record updated: data refresh {dataRefresh.DataRefreshRecordId} was {successStatus}successful.");
        }

        /// <returns>Id of new <see cref="TestDonorExportRecord"/></returns>
        /// <exception cref="Exception"></exception>
        protected async Task<int> PrepareAtlasDonorStores()
        {
            if (await LastExportIsIncomplete())
            {
                throw new Exception("Last test donor export record is incomplete; this suggests the previous export is still running " +
                                    "or that something went wrong with data refresh. See README for further guidance.");
            }

            var exportRecordId = await ExportDonors();
            await InvokeDataRefresh(exportRecordId);

            return exportRecordId;
        }

        protected abstract Task<IEnumerable<Donor>> GetTestDonors();

        private async Task<bool> LastExportIsIncomplete()
        {
            var lastRecord = await exportRepository.GetLastExportRecord();
            return lastRecord != null && lastRecord.DataRefreshCompleted == null;
        }

        private async Task<int> ExportDonors()
        {
            var recordId = await exportRepository.AddRecord();
            var donors = await GetTestDonors();
            await testDonorExporter.ExportDonorsToDonorStore(donors);
            await exportRepository.SetExportedDateTimeToNow(recordId);

            return recordId;
        }

        private async Task InvokeDataRefresh(int exportRecordId)
        {
            Debug.WriteLine("Requesting data refresh on matching algorithm.");

            var httpRequestClient = new HttpClient
            {
                // need sufficient time for request to return; note, this is not equivalent to how long data refresh takes to complete.
                Timeout = TimeSpan.FromMinutes(5)
            };

            var request = JsonConvert.SerializeObject(new DataRefreshRequest { ForceDataRefresh = true });
            var response = await httpRequestClient.PostAsync(dataRefreshRequestUrl, new StringContent(request));
            
            if(response.IsSuccessStatusCode)
            {
                var dataRefreshResponse = JsonConvert.DeserializeObject<DataRefreshResponse>(await response.Content.ReadAsStringAsync());
                Debug.WriteLine("Data refresh request submitted - check relevant AI logs for detailed progress messages.");
                await exportRepository.SetDataRefreshRecordId(exportRecordId, dataRefreshResponse.DataRefreshRecordId.Value);
                return;
            }

            Debug.WriteLine($"Data refresh request failed: {response.ReasonPhrase}");
        }
    }
}