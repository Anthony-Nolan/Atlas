using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.SubjectImport;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Settings;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace Atlas.ManualTesting.Services.Scoring
{
    public class ScoreBatchRequest
    {
        public ImportedSubject Patient { get; set; }
        public IEnumerable<ImportedSubject> Donors { get; set; }
        public string ResultsFileDirectory { get; set; }
        public ScoringCriteria ScoringCriteria { get; set; }
    }

    public interface IScoreBatchRequester
    {
        Task<IEnumerable<DonorScoringResult>> ScoreBatch(ScoreBatchRequest request);
    }

    internal class ScoreBatchRequester : IScoreBatchRequester
    {
        private static readonly HttpClient HttpRequestClient = new();
        private readonly ScoringSettings scoringSettings;

        public ScoreBatchRequester(IOptions<ScoringSettings> settings)
        {
            scoringSettings = settings.Value;
        }

        public async Task<IEnumerable<DonorScoringResult>> ScoreBatch(ScoreBatchRequest request)
        {
            if (request?.Patient is null ||
                !request.Donors.Any() ||
                string.IsNullOrWhiteSpace(request.ResultsFileDirectory) ||
                request.ScoringCriteria is null)
            {
                throw new ArgumentException("ScoreBatch request is missing required data.");
            }

            return await ScoreBatchRequest(request);
        }

        private async Task<IEnumerable<DonorScoringResult>> ScoreBatchRequest(ScoreBatchRequest scoreBatchRequest)
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);
            var request = BuildScoreBatchRequest(scoreBatchRequest);

            var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(
                    async () => await SubmitScoreBatchRequest(request, scoreBatchRequest.Patient.ID));

            if (requestResponse.Outcome == OutcomeType.Successful)
            {
                return requestResponse.Result;
            }

            await WriteFailuresToFile(scoreBatchRequest);
            return new List<DonorScoringResult>();

        }

        private static BatchScoringRequest BuildScoreBatchRequest(ScoreBatchRequest request)
        {
            return new BatchScoringRequest
            {
                PatientHla = request.Patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                DonorsHla = request.Donors.Select(ToIdentifiedDonorHla),
                ScoringCriteria = request.ScoringCriteria
            };
        }

        private static IdentifiedDonorHla ToIdentifiedDonorHla(ImportedSubject donor)
        {
            var donorHla = donor.ToPhenotypeInfo().ToPhenotypeInfoTransfer();

            return new IdentifiedDonorHla
            {
                DonorId = donor.ID,
                A = donorHla.A,
                B = donorHla.B,
                C = donorHla.C,
                Dqb1 = donorHla.Dqb1,
                Drb1 = donorHla.Drb1,
                Dpb1 = donorHla.Dpb1
            };
        }

        private async Task<IReadOnlyCollection<DonorScoringResult>> SubmitScoreBatchRequest(BatchScoringRequest scoreBatchRequest, string patientId)
        {
            var firstDonorIdInBatch = scoreBatchRequest.DonorsHla.FirstOrDefault()?.DonorId;

            try
            {
                var response = await HttpRequestClient.PostAsync(
                    scoringSettings.ScoreBatchRequestUrl, new StringContent(JsonConvert.SerializeObject(scoreBatchRequest)));
                response.EnsureSuccessStatusCode();
                var scoringResult = JsonConvert.DeserializeObject<List<DonorScoringResult>>(await response.Content.ReadAsStringAsync());

                Debug.WriteLine($"ScoreBatch result received for patient {patientId}, first donor in batch {firstDonorIdInBatch}");
                
                return scoringResult;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScoreBatch request for failed for patient {patientId}, first donor in batch {firstDonorIdInBatch}. Details: {ex.Message} " +
                                "Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }

        private static async Task WriteFailuresToFile(ScoreBatchRequest scoreBatchRequest)
        {
            var failedRequestsPath = scoreBatchRequest.ResultsFileDirectory + "\\failedScoringRequests.txt";
            var contents = $"{scoreBatchRequest.Patient.ID}:{string.Join(",", scoreBatchRequest.Donors.Select(d => d.ID))}";
            await File.AppendAllTextAsync(failedRequestsPath, contents);
        }
    }
}