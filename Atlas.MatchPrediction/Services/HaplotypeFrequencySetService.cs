using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Repositories;

namespace Atlas.MatchPrediction.Services
{
    public interface IHaplotypeFrequencySetService
    {
        Task ImportHaplotypeFrequencySet(string haplotypeFrequencySet);
    }

    public class HaplotypeFrequencySetService : IHaplotypeFrequencySetService
    {
        private readonly IHaplotypeFrequencySetImportRepository haplotypeFrequencySetImportRepository;

        public HaplotypeFrequencySetService(IHaplotypeFrequencySetImportRepository haplotypeFrequencySetImportRepository)
        {
            this
        }

    }

    /// <summary>
    /// Responsible for handling inbound donor inserts/updates.
    /// Differentiated from the `IDonorImportService` as it listens for inbound data, rather than polling an external service
    /// </summary>
    public interface IDonorService
    {
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds);
        Task CreateOrUpdateDonorBatch(IEnumerable<DonorInfo> donorInfos);
    }

    public class DonorService : IDonorService
    {
        private const string ExpansionFailureEventName = "HLA Expansion Failure(s) in Search Algorithm";

        private readonly IDonorUpdateRepository donorUpdateRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IDonorHlaExpander donorHlaExpander;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;

        public DonorService(
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory repositoryFactory,
            IDonorHlaExpander donorHlaExpander,
            IFailedDonorsNotificationSender failedDonorsNotificationSender)
        {
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            donorInspectionRepository = repositoryFactory.GetDonorInspectionRepository();
            this.donorHlaExpander = donorHlaExpander;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
        }

        public async Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds)
        {
            donorIds = donorIds.ToList();

            if (donorIds.Any())
            {
                await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(donorIds);
            }
        }
    }
