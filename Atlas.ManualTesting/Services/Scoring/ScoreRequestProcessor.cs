using Atlas.Client.Models.Search.Requests;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Models;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services.Scoring
{
    public interface IScoreRequestProcessor
    {
        /// <summary>
        /// Scores patients and donors imported from specified input files, and then writes the results to an output text file.
        /// </summary>
        Task ProcessScoreRequest(ImportAndScoreRequest request);
    }

    internal abstract class ScoreRequestProcessor : IScoreRequestProcessor
    {
        // Value should be large enough for good throughput, but small enough to avoid very large http request payload.
        private const int DonorBatchSize = 1000;
        private const string FileDelimiter = ";";
        private readonly IFileReader<ImportedSubject> subjectReader;
        private readonly IScoreBatchRequester scoreBatchRequester;

        protected ScoreRequestProcessor(IFileReader<ImportedSubject> subjectReader, IScoreBatchRequester scoreBatchRequester)
        {
            this.subjectReader = subjectReader;
            this.scoreBatchRequester = scoreBatchRequester;
        }

        /// <inheritdoc />
        public async Task ProcessScoreRequest(ImportAndScoreRequest request)
        {
            var patients = await subjectReader.ReadAllLines(FileDelimiter, request.PatientFilePath);
            var donors = await subjectReader.ReadAllLines(FileDelimiter, request.DonorFilePath);

            if (patients.Count == 0 || donors.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("Scoring request terminated early as no patient and/or donor data was read from file.");
                return;
            }

            var startFromFirstPatient = string.IsNullOrEmpty(request.StartFromPatientId);
            var startFromPatientId = startFromFirstPatient
                ? patients.First().ID
                : request.StartFromPatientId;

            var resultsFileDirectory = Path.GetDirectoryName(request.ResultsFilePath);
            var logFileDirectory = GetLogFileDirectory(request.ResultsFilePath);
            var processedPatientCount = 0;
            
            foreach (var patient in patients.SkipWhile(p => p.ID != startFromPatientId))
            {
                var startFromDonorId = processedPatientCount > 0 || startFromFirstPatient || string.IsNullOrEmpty(request.StartFromDonorId)
                    ? donors.First().ID
                    : request.StartFromDonorId;

                // Not sending out multiple donor scoring requests in parallel to keep things simple.
                // Also, it means results will be written in the order that patients and donors were read from file which makes it easier to restart the request, if required.
                foreach (var donorBatch in donors.SkipWhile(d => d.ID != startFromDonorId).Batch(DonorBatchSize))
                {
                    var donorBatchList = donorBatch.ToList();
                    var results = (await ScoreDonors(patient, donorBatchList, resultsFileDirectory, BuildScoringCriteria())).ToList();
                    await AppendToFile(results, TransformResultForReporting, patient, request.ResultsFilePath);
                    await AppendToFile(results, TransformResultForLogging, patient, $"{logFileDirectory}/{patient.ID}-{donorBatchList.First().ID}.txt");
                }

                processedPatientCount++;
            }
        }

        protected abstract ScoringCriteria BuildScoringCriteria();
        protected abstract string TransformResultForReporting(string patientId, string donorId, ScoringResult scoringResult);
        protected abstract string TransformResultForLogging(string patientId, string donorId, ScoringResult scoringResult);

        private static string GetLogFileDirectory(string resultsFilePath)
        {
            var directory = $"{Path.GetDirectoryName(resultsFilePath)}/{Path.GetFileNameWithoutExtension(resultsFilePath)}_scoringLogs";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        private static async Task AppendToFile(
            IEnumerable<DonorScoringResult> results,
            Func<string, string, ScoringResult, string> resultTransformer,
            ImportedSubject patient, string filePath)
        {
            var lines = results.Select(r => resultTransformer(patient.ID, r.DonorId, r.ScoringResult));
            await File.AppendAllLinesAsync(filePath, lines);
        }

        private async Task<IEnumerable<DonorScoringResult>> ScoreDonors(
            ImportedSubject patient,
            IEnumerable<ImportedSubject> donors,
            string resultsFileDirectory,
            ScoringCriteria scoringCriteria)
        {
            return await scoreBatchRequester.ScoreBatch(new ScoreBatchRequest
            {
                Patient = patient,
                Donors = donors,
                ResultsFileDirectory = resultsFileDirectory,
                ScoringCriteria = scoringCriteria
            });
        }
    }
}
