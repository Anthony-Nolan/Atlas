using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using Atlas.MatchPrediction.Test.Validation.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IHomeworkProcessor
    {
        Task StartOrContinueHomeworkSets(IEnumerable<int> homeworkSetIds);
    }

    internal class HomeworkProcessor : IHomeworkProcessor
    {
        private readonly IHomeworkSetRepository setRepository;
        private readonly IPatientDonorPairRepository pdpRepository;
        private readonly IPatientDonorPairProcessor pdpProcessor;

        public HomeworkProcessor(
            IHomeworkSetRepository setRepository,
            IPatientDonorPairRepository pdpRepository, 
            IPatientDonorPairProcessor pdpProcessor)
        {
            this.setRepository = setRepository;
            this.pdpRepository = pdpRepository;
            this.pdpProcessor = pdpProcessor;
        }

        public async Task StartOrContinueHomeworkSets(IEnumerable<int> homeworkSetIds)
        {
            foreach (var homeworkSetId in homeworkSetIds)
            {
                await ProcessHomeworkSet(homeworkSetId);
            }
        }

        private async Task ProcessHomeworkSet(int homeworkSetId)
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
    }
}