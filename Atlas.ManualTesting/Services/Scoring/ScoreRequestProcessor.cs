using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.ManualTesting.Common.SubjectImport;
using Atlas.ManualTesting.Models;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Extensions;

namespace Atlas.ManualTesting.Services.Scoring
{
    public class ScoreRequestProcessorInput
    {
        public ImportAndScoreRequest ImportAndScoreRequest { get; set; }
        public ScoringCriteria ScoringCriteria { get; set; }

        /// <summary>
        /// Arguments: patientId, donorId and scoring result.
        /// </summary>
        public Func<string, string, ScoringResult, string> ResultTransformer { get; set; }
    }

    public interface IScoreRequestProcessor
    {
        Task ProcessScoreRequest(ScoreRequestProcessorInput input);
    }

    internal class ScoreRequestProcessor : IScoreRequestProcessor
    {
        // Value should be large enough for good throughput, but small enough to avoid very large http request payload.
        private const int DonorBatchSize = 1000;
        private readonly ISubjectInfoReader subjectInfoReader;
        private readonly IScoreBatchRequester scoreBatchRequester;

        public ScoreRequestProcessor(ISubjectInfoReader subjectInfoReader, IScoreBatchRequester scoreBatchRequester)
        {
            this.subjectInfoReader = subjectInfoReader;
            this.scoreBatchRequester = scoreBatchRequester;
        }

        public async Task ProcessScoreRequest(ScoreRequestProcessorInput input)
        {
            var patients = await subjectInfoReader.Read(input.ImportAndScoreRequest.PatientFilePath);
            var donors = await subjectInfoReader.Read(input.ImportAndScoreRequest.DonorFilePath);

            if (patients.Count == 0 || donors.Count == 0)
            {
                Debug.WriteLine("Scoring request terminated early as no patient and/or donor data was read from file.");
                return;
            }

            var startFromFirstPatient = string.IsNullOrEmpty(input.ImportAndScoreRequest.StartFromPatientId);
            var startFromPatientId = startFromFirstPatient
                ? patients.First().ID 
                : input.ImportAndScoreRequest.StartFromPatientId;

            var resultsDirectory = Path.GetDirectoryName(input.ImportAndScoreRequest.ResultsFilePath); 
            
            var processedPatientCount = 0;
            foreach (var patient in patients.SkipWhile(p => p.ID != startFromPatientId))
            {
                var startFromDonorId = processedPatientCount > 0 || startFromFirstPatient || string.IsNullOrEmpty(input.ImportAndScoreRequest.StartFromDonorId)
                    ? donors.First().ID
                    : input.ImportAndScoreRequest.StartFromDonorId;

                // Not sending out multiple donor scoring requests in parallel to keep things simple.
                // Also, it means results will be written in the order that patients and donors were read from file which makes it easier to restart the request, if required.
                foreach (var donorBatch in donors.SkipWhile(d => d.ID != startFromDonorId).Batch(DonorBatchSize))
                {
                    var results = await ScoreDonors(patient, donorBatch, resultsDirectory, input.ScoringCriteria);
                    var transformedResults = results.Select(r => input.ResultTransformer(patient.ID, r.DonorId, r.ScoringResult));
                    await File.AppendAllLinesAsync(input.ImportAndScoreRequest.ResultsFilePath, transformedResults);
                }

                processedPatientCount++;
            }
        }

        private async Task<IEnumerable<DonorScoringResult>> ScoreDonors(
            ImportedSubject patient,
            IEnumerable<ImportedSubject> donors,
            string resultsDirectory,
            ScoringCriteria scoringCriteria)
        {
            return await scoreBatchRequester.ScoreBatch(new ScoreBatchRequest
            {
                Patient = patient,
                Donors = donors,
                ResultsDirectory = resultsDirectory,
                ScoringCriteria = scoringCriteria
            });
        }
    }
}
