using AutoMapper;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    /// <summary>
    /// Manages the addition or removal of individual donors from the search algorithm database.
    /// </summary>
    public interface IDonorManagementService
    {
        Task ManageDonorBatchByAvailability(IEnumerable<DonorAvailabilityUpdate> donorAvailabilityUpdates);
    }

    public class DonorManagementService : IDonorManagementService
    {
        private const string TraceMessagePrefix = nameof(ManageDonorBatchByAvailability);

        private readonly IDonorManagementLogRepository logRepository;
        private readonly IDonorService donorService;
        private readonly ILogger logger;
        private readonly IMapper mapper;
        private readonly IDonorManagementNotificationSender notificationSender;

        public DonorManagementService(
            IActiveRepositoryFactory repositoryFactory,
            IDonorService donorService,
            ILogger logger,
            IMapper mapper,
            IDonorManagementNotificationSender notificationSender)
        {
            logRepository = repositoryFactory.GetDonorManagementLogRepository();
            this.donorService = donorService;
            this.logger = logger;
            this.mapper = mapper;
            this.notificationSender = notificationSender;
        }

        public async Task ManageDonorBatchByAvailability(IEnumerable<DonorAvailabilityUpdate> donorAvailabilityUpdates)
        {
            var filteredUpdates = await FilterUpdates(donorAvailabilityUpdates);
            await ApplyDonorUpdates(filteredUpdates);
        }

        private async Task ApplyDonorUpdates(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var updatesList = updates.ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {updatesList.Count} donor updates to be applied.", LogLevel.Info);

            // Note, the management log must be written to last to prevent the undesirable
            // scenario of the donor update failing after the log has been successfully updated.
            await AddOrUpdateDonors(updatesList);
            await SetDonorsAsUnavailableForSearch(updatesList);
            await CreateOrUpdateManagementLogBatch(updatesList);
        }

        private async Task<IEnumerable<DonorAvailabilityUpdate>> FilterUpdates(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var latestUpdateInBatchPerDonorId = GetLatestUpdateInBatchPerDonorId(updates);

            return await GetNewerUpdatesOnly(latestUpdateInBatchPerDonorId);
        }

        private static IEnumerable<DonorAvailabilityUpdate> GetLatestUpdateInBatchPerDonorId(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            return updates
                .GroupBy(u => u.DonorId)
                .Select(grp => grp.OrderByDescending(u => u.UpdateSequenceNumber).First());
        }

        /// <returns>Only returns those updates that are newer than the last update recorded in the
        /// donor management log, or those where the donor has no record of a previous update.</returns>
        private async Task<IEnumerable<DonorAvailabilityUpdate>> GetNewerUpdatesOnly(
            IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var allUpdates = updates.ToList();

            var existingLogs = await logRepository.GetDonorManagementLogBatch(allUpdates.Select(u => u.DonorId));

            // GroupJoin is equivalent to a LEFT OUTER JOIN
            var updateStatuses = allUpdates
                .GroupJoin(existingLogs,
                    update => update.DonorId,
                    log => log.DonorId,
                    (update, logs) => new { Update = update, Log = logs.SingleOrDefault() })
                .Select(a => new
                {
                    a.Update,
                    IsNewer = a.Update.UpdateSequenceNumber > (a.Log?.SequenceNumberOfLastUpdate ?? 0)
                })
                .ToList();

            await SendNotificationForNonApplicableUpdates(
                updateStatuses.Where(u => !u.IsNewer).Select(u => u.Update));

            return updateStatuses.Where(u => u.IsNewer).Select(u => u.Update);
        }

        private async Task SendNotificationForNonApplicableUpdates(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var updatesList = updates.ToList();

            if (updatesList.Any())
            {
                await notificationSender.SendDonorUpdatesNotAppliedNotification(updatesList);
            }
        }

        private async Task AddOrUpdateDonors(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var availableDonors = updates
                .Where(update => update.IsAvailableForSearch)
                .Select(d => d.DonorInfo)
                .ToList();

            if (availableDonors.Any())
            {
                logger.SendTrace($"{TraceMessagePrefix}: {availableDonors.Count} donors to be added or updated.", LogLevel.Info);

                await donorService.CreateOrUpdateDonorBatch(availableDonors);
            }
        }

        private async Task SetDonorsAsUnavailableForSearch(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var unavailableDonorIds = updates
                .Where(update => !update.IsAvailableForSearch)
                .Select(d => d.DonorId)
                .ToList();

            if (unavailableDonorIds.Any())
            {
                logger.SendTrace($"{TraceMessagePrefix}: {unavailableDonorIds.Count} donors to be marked as unavailable for search.", LogLevel.Info);

                await donorService.SetDonorAsUnavailableForSearchBatch(unavailableDonorIds);
            }
        }

        private async Task CreateOrUpdateManagementLogBatch(IEnumerable<DonorAvailabilityUpdate> appliedUpdates)
        {
            if (!appliedUpdates.Any())
            {
                return;
            }

            var infos = mapper.Map<IEnumerable<DonorManagementInfo>>(appliedUpdates);
            await logRepository.CreateOrUpdateDonorManagementLogBatch(infos);
        }
    }
}
