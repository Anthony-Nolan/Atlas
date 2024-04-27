using System;
using System.Linq;
using System.Threading.Tasks;
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

        public HomeworkRequestProcessor(
            IHomeworkSetRepository setRepository,
            IPatientDonorPairRepository pdpRepository)
        {
            this.setRepository = setRepository;
            this.pdpRepository = pdpRepository;
        }

        /// <inheritdoc />
        public async Task<int> StoreHomeworkRequest(HomeworkRequest request)
        {
            var setId = await setRepository.Add(request.HomeworkSetName, request.ResultsPath, request.MatchLoci.MatchLociToString());
            await pdpRepository.BulkInsert(request.PatientDonorPairs.Select(pdp => MapToDatabaseModel(pdp, setId)).ToList());
            return setId;
        }

        public Task StartOrContinueHomeworkRequest(int homeworkSetId)
        {
            throw new NotImplementedException();
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