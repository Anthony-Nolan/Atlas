using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4
{
    public interface ISearchRequester
    {
        /// <summary>
        /// Submits search requests for all imported patients.
        /// </summary>
        /// <returns>Search set ID</returns>
        Task<int> SubmitSearchRequests(ValidationSearchRequest request);
    }

    internal class SearchRequester : ISearchRequester
    {
        private static readonly HttpClient HttpRequestClient = new();

        private readonly ITestDonorExportRepository testDonorExportRepository;
        private readonly ISearchSetRepository searchSetRepository;
        private readonly ISearchRequestsRepository<ValidationSearchRequestRecord> searchRequestsRepository;
        private readonly ISubjectRepository subjectRepository;
        private readonly string searchRequestUrl;

        public SearchRequester(
            ITestDonorExportRepository testDonorExportRepository,
            ISearchSetRepository searchSetRepository,
            ISearchRequestsRepository<ValidationSearchRequestRecord> searchRequestsRepository,
            ISubjectRepository subjectRepository,
            IOptions<ValidationSearchSettings> settings)
        {
            this.testDonorExportRepository = testDonorExportRepository;
            this.searchSetRepository = searchSetRepository;
            this.searchRequestsRepository = searchRequestsRepository;
            this.subjectRepository = subjectRepository;
            this.searchRequestUrl = settings.Value.RequestUrl;
        }

        public async Task<int> SubmitSearchRequests(ValidationSearchRequest request)
        {
            var lastExportId = await LastTestDonorExportId();

            if (lastExportId == null)
            {
                throw new Exception("Cannot submit search requests as the last test donor export record is incomplete.");
            }

            var searchSetId = await searchSetRepository.Add(
                lastExportId.Value,
                request.DonorType.ToString(),
                request.MismatchCount,
                request.MatchLoci.MatchLociToString());

            await SubmitSearchRequests(searchSetId, request);

            return searchSetId;
        }

        private async Task<int?> LastTestDonorExportId()
        {
            var lastRecord = await testDonorExportRepository.GetLastExportRecord();
            return lastRecord?.WasDataRefreshSuccessful == null || !lastRecord.WasDataRefreshSuccessful.Value
                ? null
                : lastRecord.Id;
        }

        private async Task SubmitSearchRequests(int searchSetId, ValidationSearchRequest request)
        {
            var patients = await subjectRepository.GetPatients();
            var mismatchCriteria = BuildMismatchCriteria(request);

            foreach (var patient in patients)
            {
                await RunAndStoreSearchRequests(searchSetId, patient, request.DonorType, mismatchCriteria);
            }

            await searchSetRepository.MarkSearchSetAsComplete(searchSetId);
        }

        private static MismatchCriteria BuildMismatchCriteria(ValidationSearchRequest request)
        {
            var locusMismatchCriteria = request.MatchLoci
                .ToLociInfo()
                .Map((_, isMatchLocus) => isMatchLocus ? request.MismatchCount : (int?)null)
                .ToLociInfoTransfer();

            return new MismatchCriteria
            {
                DonorMismatchCount = request.MismatchCount,
                LocusMismatchCriteria = locusMismatchCriteria,
                IncludeBetterMatches = false
            };
        }

        private async Task RunAndStoreSearchRequests(int searchSetId, SubjectInfo patient, DonorType donorType, MismatchCriteria mismatchCriteria)
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);
            var searchRequest = BuildSearchRequest(patient, donorType, mismatchCriteria);
            var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(
                async () => await SubmitSearchRequest(searchRequest, patient.Id));

            var searchFailed = requestResponse.Outcome == OutcomeType.Failure;
            const string failedSearchId = "FAILED-SEARCH";

            await searchRequestsRepository.AddSearchRequest(new ValidationSearchRequestRecord
            {
                SearchSet_Id = searchSetId,
                PatientId = patient.Id,
                DonorMismatchCount = searchRequest.MatchCriteria.DonorMismatchCount,
                AtlasSearchIdentifier = searchFailed ? failedSearchId : requestResponse.Result,
                WasSuccessful = searchFailed ? false : null
            });
        }

        private static SearchRequest BuildSearchRequest(SubjectInfo patient, DonorType donorType, MismatchCriteria mismatchCriteria)
        {
            var patientHla = patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer();
            return new SearchRequest
            {
                SearchHlaData = patientHla,
                SearchDonorType = donorType,
                MatchCriteria = mismatchCriteria,
                ScoringCriteria = new ScoringCriteria
                {
                    LociToScore = LociToScore(patientHla),
                    LociToExcludeFromAggregateScore = new List<Locus>()
                },
                PatientEthnicityCode = $"{patient.ExternalHfSetId}",
                PatientRegistryCode = $"{patient.ExternalHfSetId}",
                RunMatchPrediction = true
            };
        }

        private static List<Locus> LociToScore(PhenotypeInfoTransfer<string> patientHla)
        {
            return patientHla.ToPhenotypeInfo().Reduce((locus, info, lociToScore) =>
            {
                if (info.Position1And2NotNull())
                {
                    lociToScore.Add(locus);
                }
                return lociToScore;
            }, new List<Locus>());
        }

        private async Task<string> SubmitSearchRequest(SearchRequest searchRequest, int patientId)
        {
            try
            {
                var response = await HttpRequestClient.PostAsync(
                    searchRequestUrl, new StringContent(JsonConvert.SerializeObject(searchRequest)));
                response.EnsureSuccessStatusCode();

                var searchResponse = JsonConvert.DeserializeObject<SearchInitiationResponse>(await response.Content.ReadAsStringAsync());
                System.Diagnostics.Debug.WriteLine($"Search request submitted for {patientId} with request id: {searchResponse.SearchIdentifier}.");
                return searchResponse.SearchIdentifier;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search request failed for {patientId}. Details: {ex.Message} " +
                                "Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }
    }
}