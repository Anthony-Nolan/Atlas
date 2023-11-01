using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;
using Microsoft.Extensions.Options;
using MoreLinq;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MatchPredictionRequest = Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionRequest;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise3
{
    public interface IMatchPredictionRequester
    {
        /// <summary>
        /// Starts sending match prediction requests from first patient onwards; involves full wipe down of previous results.
        /// </summary>
        Task SendMatchPredictionRequests();

        /// <summary>
        /// Starts sending match prediction requests from the last patient found to have a match request, onwards.
        /// Only the results for that patient will be removed and the patient re-processed; all other results will remain.
        /// </summary>
        Task ResumeMatchPredictionRequests();
    }

    internal class MatchPredictionRequester : IMatchPredictionRequester
    {
        private static readonly HttpClient HttpRequestClient = new();
        private readonly IValidationRepository validationRepository;
        private readonly ISubjectRepository subjectRepository;
        private readonly IMatchPredictionRequestRepository requestRepository;
        private readonly string requestUrl;
        private readonly int batchSize;

        public MatchPredictionRequester(
            IValidationRepository validationRepository,
            ISubjectRepository subjectRepository,
            IMatchPredictionRequestRepository requestRepository,
            IOptions<OutgoingMatchPredictionRequestSettings> settings)
        {
            this.validationRepository = validationRepository;
            this.subjectRepository = subjectRepository;
            this.requestRepository = requestRepository;
            requestUrl = settings.Value.RequestUrl;
            batchSize = settings.Value.RequestBatchSize;
        }

        /// <inheritdoc />
        public async Task SendMatchPredictionRequests()
        {
            await BatchRequestMatchPrediction(0);
        }

        /// <inheritdoc />
        public async Task ResumeMatchPredictionRequests()
        {
            var maxPatientId = await requestRepository.GetMaxPatientId();
            await BatchRequestMatchPrediction(maxPatientId);
        }

        private async Task BatchRequestMatchPrediction(int fromPatientId)
        {
            await validationRepository.DeleteMatchPredictionRelatedData(fromPatientId);

            Debug.WriteLine($"Starting to send match prediction requests from patient id {fromPatientId}...");

            var patients = await subjectRepository.GetPatients(fromPatientId);
            var donors = (await subjectRepository.GetDonors()).ToList();

            foreach (var patient in patients)
            {
                var sendTasks = donors
                    .Batch(batchSize)
                    .Select(d => SendAndStoreMatchPredictionRequests(patient, d));
                await Task.WhenAll(sendTasks);

                Debug.WriteLine($"Requests submitted for {patient.ExternalId}");
            }

            Debug.WriteLine("Completed sending match prediction requests.");
        }

        private async Task SendAndStoreMatchPredictionRequests(SubjectInfo patient, IEnumerable<SubjectInfo> donors)
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);

            var batchRequest = BuildBatchRequest(patient, donors);
            var response = await retryPolicy.ExecuteAndCaptureAsync(
                    async () => await PostRequest(batchRequest, patient.ExternalId));

            if (response.Outcome == OutcomeType.Failure)
            {
                // best to throw and disrupt the run as the most likely exceptions are either too many messages on the request topic
                // or request validation failures, both of which should be resolved before continuing with the run
                throw new Exception($"Error when sending match prediction requests for patient {patient.ExternalId}", response.FinalException);
            }

            var requestInfo = response.Result?.DonorResponses
                .Select(d => BuildRequestForStorage(patient.Id, d))
                .ToList();

            await requestRepository.BulkInsert(requestInfo);
        }

        private static BatchedMatchPredictionRequests BuildBatchRequest(SubjectInfo patient, IEnumerable<SubjectInfo> donors)
        {
            var donorsInfo = donors.Select(d => new Donor
            {
                Id = d.Id,
                FrequencySetMetadata = new FrequencySetMetadata(),
                Hla = d.ToPhenotypeInfo().ToPhenotypeInfoTransfer()
            });

            return new BatchedMatchPredictionRequests
            {
                ExcludedLoci = new List<Locus> { Locus.Dpb1 },
                PatientFrequencySetMetadata = new FrequencySetMetadata(),
                PatientHla = patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                Donors = donorsInfo
            };
        }

        private async Task<BatchedMatchPredictionInitiationResponse> PostRequest(BatchedMatchPredictionRequests request, string patientId)
        {
            try
            {
                var response = await HttpRequestClient.PostAsync(
                requestUrl, new StringContent(JsonConvert.SerializeObject(request)));
                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<BatchedMatchPredictionInitiationResponse>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Request failed for {patientId}. Details: {ex.Message}. {ex.InnerException} " +
                                "Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }

        private static MatchPredictionRequest BuildRequestForStorage(int internalPatientId, DonorResponse donor)
        {
            return new MatchPredictionRequest
            {
                PatientId = internalPatientId,
                DonorId = donor.DonorId ?? throw new ArgumentNullException(nameof(donor.DonorId)),
                MatchPredictionAlgorithmRequestId = donor.MatchPredictionRequestId,
                RequestErrors = donor.ValidationErrors?.ToString()
            };
        }
    }
}