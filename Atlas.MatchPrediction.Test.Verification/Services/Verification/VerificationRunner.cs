using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Verification.Config;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
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
        private static readonly HttpClient HttpRequestClient = new();

        private readonly ITestDonorExportRepository exportRepository;
        private readonly ITestHarnessRepository harnessRepository;
        private readonly IHaplotypeFrequencySetReader setReader;
        private readonly IVerificationRunRepository verificationRunRepository;
        private readonly ISimulantsRepository simulantsRepository;
        private readonly ISearchRequestsRepository<VerificationSearchRequestRecord> searchRequestsRepository;
        private readonly string searchRequestUrl;

        public VerificationRunner(
            ITestDonorExportRepository exportRepository,
            ITestHarnessRepository harnessRepository,
            IHaplotypeFrequencySetReader setReader,
            IVerificationRunRepository verificationRunRepository,
            ISimulantsRepository simulantsRepository,
            ISearchRequestsRepository<VerificationSearchRequestRecord> searchRequestsRepository,
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
            var lastExportRecord = await exportRepository.GetLastExportRecord();
            var harness = await harnessRepository.GetTestHarness(testHarnessId);

            return lastExportRecord == null
                || harness.ExportRecord_Id == null
                || harness.ExportRecord_Id != lastExportRecord.Id;
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
            var typingCategory = await harnessRepository.GetTypingCategoryOfGenotypesInTestHarness(testHarnessId);
            var patients = await simulantsRepository.GetSimulants(testHarnessId, TestIndividualCategory.Patient.ToString());

            foreach (var patient in patients)
            {
                await RunAndStoreSearchRequests(verificationRunId, patient, typingCategory);
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

        private async Task RunAndStoreSearchRequests(int verificationRunId, Simulant patient, ImportTypingCategory typingCategory)
        {
            const string failedSearchId = "FAILED-SEARCH";
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);
            var searchRequest = BuildFiveLocusMismatchSearchRequest(patient, typingCategory);

            var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(
                    async () => await SubmitSearchRequest(searchRequest, patient.Id));

            var searchFailed = requestResponse.Outcome == OutcomeType.Failure;

            await searchRequestsRepository.AddSearchRequest(new VerificationSearchRequestRecord
            {
                VerificationRun_Id = verificationRunId,
                PatientId = patient.Id,
                DonorMismatchCount = searchRequest.MatchCriteria.DonorMismatchCount,
                WasMatchPredictionRun = searchRequest.RunMatchPrediction,
                AtlasSearchIdentifier = searchFailed ? failedSearchId : requestResponse.Result,
                WasSuccessful = searchFailed ? false : null
            });
        }

        /// <returns>
        /// Donor mismatch count - when patient is <see cref="SimulatedHlaTypingCategory.Genotype"/>: 5; else: 2.
        /// Run Match prediction - when patient is <see cref="SimulatedHlaTypingCategory.Masked"/>
        ///     OR when <paramref name="typingCategory"/> is not small G group, then true; else false.
        /// </returns>
        private static SearchRequest BuildFiveLocusMismatchSearchRequest(Simulant patient, ImportTypingCategory typingCategory)
        {
            const int locusMismatchCount = 2;

            var isMasked = patient.SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Masked;
            var isNotSmallG = typingCategory != ImportTypingCategory.SmallGGroup;

            return new SearchRequest
            {
                SearchHlaData = patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                SearchDonorType = patient.SimulatedHlaTypingCategory.ToDonorType(),
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = isMasked ? 2 : 5,
                    LocusMismatchCriteria = new LociInfoTransfer<int?>
                    {
                        A = locusMismatchCount,
                        B = locusMismatchCount,
                        C = locusMismatchCount,
                        Dqb1 = locusMismatchCount,
                        Drb1 = locusMismatchCount
                    },
                    IncludeBetterMatches = true
                },
                // no scoring requested
                ScoringCriteria = new ScoringCriteria
                {
                    LociToExcludeFromAggregateScore = new List<Locus>(),
                    LociToScore = new List<Locus>()
                },
                // ensure global HF set is selected
                PatientEthnicityCode = null,
                PatientRegistryCode = null,

                RunMatchPrediction = isMasked || isNotSmallG
            };
        }

        private async Task<string> SubmitSearchRequest(SearchRequest searchRequest, int patientId)
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