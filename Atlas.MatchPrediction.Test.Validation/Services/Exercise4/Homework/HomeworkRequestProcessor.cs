using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using Atlas.MatchPrediction.Test.Validation.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IHomeworkRequestProcessor
    {
        /// <returns>Homework Set Id</returns>
        Task<int> StoreHomeworkRequest(HomeworkRequest request);

        Task StartOrContinueHomeworkRequest(int homeworkSetId);
    }

    internal class HomeworkRequestProcessor : IHomeworkRequestProcessor
    {
        private const string FileDelimiter = ",";
        private readonly IHomeworkSetRepository setRepository;
        private readonly IFileReader<SubjectIdPair> fileReader;
        private readonly IPatientDonorPairRepository pdpRepository;
        private readonly IPatientDonorPairProcessor pdpProcessor;

        public HomeworkRequestProcessor(
            IHomeworkSetRepository setRepository,
            IFileReader<SubjectIdPair> fileReader,
            IPatientDonorPairRepository pdpRepository, 
            IPatientDonorPairProcessor pdpProcessor)
        {
            this.setRepository = setRepository;
            this.fileReader = fileReader;
            this.pdpRepository = pdpRepository;
            this.pdpProcessor = pdpProcessor;
        }

        /// <inheritdoc />
        public async Task<int> StoreHomeworkRequest(HomeworkRequest request)
        {
            var subjectIdPairs = await fileReader.ReadAllLines(
                FileDelimiter,
                Path.Combine(request.InputPath, request.SetFileName),
                hasHeaderRecord: false);

            if (subjectIdPairs.IsNullOrEmpty())
            {
                throw new ArgumentException($"No patient-donor pairs found in file {request.SetFileName}.");
            }

            var setId = await setRepository.Add(request.SetFileName, request.ResultsPath, request.MatchLoci.MatchLociToString());

            await pdpRepository.BulkInsert(subjectIdPairs.Select(ids => MapToDatabaseModel(ids, setId)).ToList());
            return setId;
        }

        public async Task StartOrContinueHomeworkRequest(int homeworkSetId)
        {
            var set = await setRepository.Get(homeworkSetId);

            if (set == null)
            {
                throw new ArgumentException($"No homework set found with id {homeworkSetId}.");
            }

            var pdps = (await pdpRepository.GetUnprocessedPairs(homeworkSetId)).ToList();

            if (pdps.IsNullOrEmpty())
            {
                System.Diagnostics.Debug.WriteLine($"No unprocessed patient-donor pairs found for homework set {homeworkSetId}.");
                return;
            }

            var matchLoci = set.MatchLoci.ToLociInfo();

            foreach (var pdp in pdps)
            {
                await pdpProcessor.Process(pdp, matchLoci);
            }
        }

        private static PatientDonorPair MapToDatabaseModel(SubjectIdPair ids, int homeworkSetId)
        {
            return new PatientDonorPair
            {
                PatientId = ids.PatientId,
                DonorId = ids.DonorId,
                HomeworkSet_Id = homeworkSetId
            };
        }
    }
}