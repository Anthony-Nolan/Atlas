using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
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
        private readonly IHomeworkSetRepository setRepository;
        private readonly IPatientDonorPairRepository pdpRepository;
        private readonly IPatientDonorPairProcessor pdpProcessor;

        public HomeworkRequestProcessor(
            IHomeworkSetRepository setRepository,
            IPatientDonorPairRepository pdpRepository, 
            IPatientDonorPairProcessor pdpProcessor)
        {
            this.setRepository = setRepository;
            this.pdpRepository = pdpRepository;
            this.pdpProcessor = pdpProcessor;
        }

        /// <inheritdoc />
        public async Task<int> StoreHomeworkRequest(HomeworkRequest request)
        {
            var setId = await setRepository.Add(request.HomeworkSetName, request.ResultsPath, request.MatchLoci.MatchLociToString());
            await pdpRepository.BulkInsert(request.PatientDonorPairs.Select(pdp => MapToDatabaseModel(pdp, setId)).ToList());
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

        private static PatientDonorPair MapToDatabaseModel(string patientDonorPair, int homeworkSetId)
        {
            var ids = patientDonorPair.Split(',');

            if (ids.Length != 2)
            {
                throw new ArgumentException($"{nameof(patientDonorPair)} must contain exactly two ids separated by a comma; instead found {patientDonorPair}.");
            }

            return new PatientDonorPair
            {
                PatientId = ids[0],
                DonorId = ids[1],
                HomeworkSet_Id = homeworkSetId
            };
        }
    }
}