using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Config;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    public interface IVerificationRunner
    {
        /// <returns>Id for Verification Run</returns>
        Task<int> SendVerificationSearchRequests(int testHarnessId);
    }

    internal class VerificationRunner : IVerificationRunner
    {
        private static readonly HttpClient HttpRequestClient = new HttpClient();

        private readonly ITestDonorExportRepository exportRepository;
        private readonly ITestHarnessRepository harnessRepository;
        private readonly IHaplotypeFrequencySetReader setReader;
        private readonly IVerificationRunRepository verificationRunRepository;
        private readonly ISimulantsRepository simulantsRepository;
        private readonly ISearchRequestsRepository searchRequestsRepository;
        private readonly string searchRequestUrl;

        public VerificationRunner(
            ITestDonorExportRepository exportRepository,
            ITestHarnessRepository harnessRepository,
            IHaplotypeFrequencySetReader setReader,
            IVerificationRunRepository verificationRunRepository,
            ISimulantsRepository simulantsRepository,
            ISearchRequestsRepository searchRequestsRepository,
            IOptions<VerificationSearchSettings> settings)
        {
            this.exportRepository = exportRepository;
            this.harnessRepository = harnessRepository;
            this.setReader = setReader;
            this.verificationRunRepository = verificationRunRepository;
            this.simulantsRepository = simulantsRepository;
            this.searchRequestsRepository = searchRequestsRepository;
            searchRequestUrl = settings.Value.RequestUrl;
        }

        public async Task<int> SendVerificationSearchRequests(int testHarnessId)
        {
            if (await TestHarnessDonorsNotOnAtlasDonorStores(testHarnessId))
            {
                throw new ArgumentException($"Donors for test harness {testHarnessId} are not on the Atlas donor stores. " +
                                            $"Run {nameof(Functions.AtlasPreparationFunctions.PrepareAtlasDonorStores)} function to export test donors. " +
                                            "Consult README for more detailed guidance.");
            }

            if (await TestHarnessHaplotypeFrequencySetNotActiveOnAtlas(testHarnessId))
            {
                throw new ArgumentException("The global haplotype frequency set that is currently active on Atlas differs" +
                                            $" to that used to generate test harness {testHarnessId}. These must align for " +
                                            "verification results to be useful.");
            }

            Debug.WriteLine($"Start submitting search requests for test harness {testHarnessId}...");

            var verificationRunId = await SubmitSearchRequests(testHarnessId);

            Debug.WriteLine("Completed submitting search requests.");

            return verificationRunId;
        }

        private async Task<bool> TestHarnessDonorsNotOnAtlasDonorStores(int testHarnessId)
        {
            var lastExport = await exportRepository.GetLastExportRecord();

            return lastExport == null || lastExport.TestHarness_Id != testHarnessId;
        }

        private async Task<bool> TestHarnessHaplotypeFrequencySetNotActiveOnAtlas(int testHarnessId)
        {
            var setId = await harnessRepository.GetHaplotypeFrequencySetIdOfTestHarness(testHarnessId);
            var activeSet = await setReader.GetActiveGlobalHaplotypeFrequencySet();

            return activeSet == null || setId != activeSet.Id;
        }

        private async Task<int> SubmitSearchRequests(int testHarnessId)
        {
            var verificationRunId = await AddVerificationRun(testHarnessId);
            var patients = await simulantsRepository.GetSimulants(testHarnessId, TestIndividualCategory.Patient.ToString());

            foreach (var patient in patients)
            {
                await RunAndStoreSearchRequests(verificationRunId, patient);
            }

            await verificationRunRepository.MarkSearchRequestsAsSubmitted(verificationRunId);

            return verificationRunId;
        }

        private async Task<int> AddVerificationRun(int testHarnessId)
        {
            return await verificationRunRepository.AddVerificationRun(new VerificationRun
            {
                TestHarness_Id = testHarnessId,
                SearchLociCount = VerificationConstants.SearchLociCount
            });
        }

        private async Task RunAndStoreSearchRequests(int verificationRunId, Simulant patient)
        {
            const string failedSearchId = "FAILED-SEARCH";
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);
            var searchRequests = BuildFiveLocusMismatchSearchRequests(patient);

            foreach (var searchRequest in searchRequests)
            {
                var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(
                    async () => await SubmitSingleSearch(searchRequest, patient.Id));

                await searchRequestsRepository.AddSearchRequest(new SearchRequestRecord
                {
                    VerificationRun_Id = verificationRunId,
                    PatientSimulant_Id = patient.Id,
                    DonorMismatchCount = searchRequest.MatchCriteria.DonorMismatchCount,
                    AtlasSearchIdentifier = requestResponse.Outcome == OutcomeType.Failure ? failedSearchId : requestResponse.Result,
                    WasSuccessful = requestResponse.Outcome == OutcomeType.Failure ? false : (bool?)null
                });
            }
        }

        /// <returns>
        /// When patient is <see cref="SimulatedHlaTypingCategory.Genotype"/>: 5+/10 search request; else: 8+/10 search requests.
        /// </returns>
        private static IEnumerable<SearchRequest> BuildFiveLocusMismatchSearchRequests(Simulant patient)
        {
            const int locusMismatchCount = 2;

            // TODO ATLAS-340: Refactor this to a single int after Adult searches can be run as "exact or better".
            // I.e., mismatch count will be either '5' for Genotype, or '2' for Masked.
            var donorMismatchCounts = patient.SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Genotype
                ? new[] { 5 }
                : new[] { 0, 1, 2 };

            return donorMismatchCounts.Select(mm => new SearchRequest
            {
                SearchHlaData = patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                SearchDonorType = VerificationConstants.GetSearchDonorType(patient.SimulatedHlaTypingCategory),
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = mm,
                    LocusMismatchCriteria = new LociInfoTransfer<int?>
                    {
                        A = locusMismatchCount,
                        B = locusMismatchCount,
                        C = locusMismatchCount,
                        Dqb1 = locusMismatchCount,
                        Drb1 = locusMismatchCount
                    }
                },
                // no scoring requested
                ScoringCriteria = new ScoringCriteria
                {
                    LociToExcludeFromAggregateScore = new List<Locus>(),
                    LociToScore = new List<Locus>()
                },
                // ensure global HF set is selected
                PatientEthnicityCode = null,
                PatientRegistryCode = null
            });
        }

        private async Task<string> SubmitSingleSearch(SearchRequest searchRequest, int patientId)
        {
            try
            {
                var response = await HttpRequestClient.PostAsync(
                searchRequestUrl, new StringContent(JsonConvert.SerializeObject(searchRequest)));
                response.EnsureSuccessStatusCode();

                var searchResponse = JsonConvert.DeserializeObject<SearchInitiationResponse>(await response.Content.ReadAsStringAsync());
                Debug.WriteLine($"Search request (mm:{searchRequest.MatchCriteria.DonorMismatchCount}) submitted for {patientId} " +
                                $"with request id: {searchResponse.SearchIdentifier}.");
                return searchResponse.SearchIdentifier;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search request (mm:{searchRequest.MatchCriteria.DonorMismatchCount}) failed for {patientId}. Details: {ex.Message} " +
                                "Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }
    }
}