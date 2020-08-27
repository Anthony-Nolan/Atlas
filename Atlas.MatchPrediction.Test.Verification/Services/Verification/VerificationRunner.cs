using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    public interface IVerificationRunner
    {
        Task SendVerificationSearchRequests(int testHarnessId);
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

        public async Task SendVerificationSearchRequests(int testHarnessId)
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

            await SubmitSearchRequests(testHarnessId);

            Debug.WriteLine("Completed submitting search requests.");
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

        private async Task SubmitSearchRequests(int testHarnessId)
        {
            const int searchLociCount = 5;
            var verificationId = await AddVerificationRun(testHarnessId, searchLociCount, BuildFiveLocusMismatchSearchRequest(null));
            var patients = await simulantsRepository.GetSimulants(testHarnessId, TestIndividualCategory.Patient.ToString());

            foreach (var patient in patients)
            {
                var atlasId = await SubmitSingleSearch(patient);
                await searchRequestsRepository.AddSearchRequest(new SearchRequestRecord
                {
                    VerificationRun_Id = verificationId,
                    PatientSimulant_Id = patient.Id,
                    AtlasSearchIdentifier = atlasId
                });
            }

            await verificationRunRepository.MarkSearchRequestsAsSubmitted(verificationId);
        }

        private async Task<int> AddVerificationRun(int testHarnessId, int searchLociCount, SearchRequest searchRequest)
        {
            return await verificationRunRepository.AddVerificationRun(new VerificationRun
            {
                TestHarness_Id = testHarnessId,
                SearchLociCount = searchLociCount,
                SearchRequest = JsonConvert.SerializeObject(searchRequest),
            });
        }

        private async Task<string> SubmitSingleSearch(Simulant patient)
        {
            const string failedSearchText = "FAILED-SEARCH";

            try
            {
                var searchRequest = JsonConvert.SerializeObject(
                BuildFiveLocusMismatchSearchRequest(patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer()));

                var result = await HttpRequestClient.PostAsync(searchRequestUrl, new StringContent(searchRequest));
                result.EnsureSuccessStatusCode();

                var searchResponse = JsonConvert.DeserializeObject<SearchInitiationResponse>(await result.Content.ReadAsStringAsync());
                Debug.WriteLine($"Search request submitted for {patient.Id} with request id: {searchResponse.SearchIdentifier}.");
                return searchResponse.SearchIdentifier;
            }
            catch (Exception)
            {
                Debug.WriteLine($"Search request failed for {patient.Id}.");
                return failedSearchText;
            }
        }

        private static SearchRequest BuildFiveLocusMismatchSearchRequest(PhenotypeInfoTransfer<string> searchHla)
        {
            const int mismatchCount = 2;

            return new SearchRequest
            {
                SearchHlaData = searchHla,
                SearchDonorType = DonorType.Cord,
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = mismatchCount,
                    LocusMismatchCriteria = new LociInfoTransfer<int?>
                    {
                        A = mismatchCount,
                        B = mismatchCount,
                        C = mismatchCount,
                        Dqb1 = mismatchCount,
                        Drb1 = mismatchCount
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
            };
        }
    }
}
