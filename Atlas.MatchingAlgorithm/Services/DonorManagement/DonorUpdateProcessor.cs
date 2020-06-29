using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Services.DonorManagement
{
    // Normally we'd put interfaces etc. in their own files.
    // However these interfaces and classes are A) tiny, and B) exist exclusively to serve this file. So they're more useful, convenient and clear if they live here.
    public interface IMessageProcessorForDbADonorUpdates : IMessageProcessor<SearchableDonorUpdate> { }
    public interface IMessageProcessorForDbBDonorUpdates : IMessageProcessor<SearchableDonorUpdate> { }

    public class DonorUpdateMessageProcessor
        : MessageProcessor<SearchableDonorUpdate>,
          IMessageProcessorForDbADonorUpdates,
          IMessageProcessorForDbBDonorUpdates
    {
        public DonorUpdateMessageProcessor(IServiceBusMessageReceiver<SearchableDonorUpdate> messageReceiver)
            : base(messageReceiver)
        { }
    }

    internal enum DatabaseStateWithRespectToDonorUpdates
    {
        Active,
        Refreshing,
        Dormant
    }


    public interface IDonorUpdateProcessor
    {
        Task ProcessDifferentialDonorUpdates(TransientDatabase targetDatabase);
    }
    
    public class DonorUpdateProcessor : IDonorUpdateProcessor
    {
        private const string TraceMessagePrefix = nameof(ProcessDifferentialDonorUpdates);

        private readonly IMessageProcessorForDbADonorUpdates dbAMessageProcessorService;
        private readonly IMessageProcessorForDbBDonorUpdates dbBMessageProcessorService;
        private readonly IDataRefreshHistoryRepository refreshHistoryRepository;
        private readonly IDonorManagementService donorManagementService;
        private readonly ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private readonly ILogger logger;
        private readonly int batchSize;

        public DonorUpdateProcessor(
            IMessageProcessorForDbADonorUpdates dbAMessageProcessorService,
            IMessageProcessorForDbBDonorUpdates dbBMessageProcessorService,
            IDataRefreshHistoryRepository refreshHistoryRepository,
            IDonorManagementService donorManagementService,
            ISearchableDonorUpdateConverter searchableDonorUpdateConverter,
            ILogger logger,
            int batchSize)
        {
            this.dbAMessageProcessorService = dbAMessageProcessorService;
            this.dbBMessageProcessorService = dbBMessageProcessorService;
            this.refreshHistoryRepository = refreshHistoryRepository;
            this.donorManagementService = donorManagementService;
            this.searchableDonorUpdateConverter = searchableDonorUpdateConverter;
            this.logger = logger;
            this.batchSize = batchSize;
        }

        public async Task ProcessDifferentialDonorUpdates(TransientDatabase targetDatabase)
        {
            var targetDatabaseState = DetermineDatabaseState(targetDatabase);
            var messageProcessorService = ChooseMessagesToProcess(targetDatabase);

            switch (targetDatabaseState)
            {
                case DatabaseStateWithRespectToDonorUpdates.Active:
                    await messageProcessorService.ProcessMessageBatchAsync(
                        async batch => await ProcessMessages(batch, targetDatabase),
                        batchSize,
                        batchSize * 2);
                    return;

                case DatabaseStateWithRespectToDonorUpdates.Dormant:
                    await messageProcessorService.ProcessMessageBatchAsync(
                        async batch => await DiscardMessages(batch),
                        batchSize * 10,
                        batchSize * 20);
                    return;

                case DatabaseStateWithRespectToDonorUpdates.Refreshing:
                    // Do nothing!
                    // We want the messages to be left in the queue.
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private DatabaseStateWithRespectToDonorUpdates DetermineDatabaseState(TransientDatabase targetDatabase)
        {
            var activeDb = refreshHistoryRepository.GetActiveDatabase();
            var refreshJobs = refreshHistoryRepository.GetInProgressJobs().ToList();
            var dbBeingRefreshed = refreshJobs.FirstOrDefault()?.Database?.ParseToEnum<TransientDatabase>();
            
            var targetDbIsActive = (activeDb == targetDatabase);
            var otherDbIsActive = (activeDb != null && activeDb != targetDatabase);
            var noActiveDbExists = (activeDb == null);
            var refreshesAreRunning = refreshJobs.Any();
            var targetIsBeingRefreshed = (dbBeingRefreshed == targetDatabase);
            var multipleRefreshesAreRunning = refreshJobs.Count > 1;

            if (targetDbIsActive                        && !targetIsBeingRefreshed && !multipleRefreshesAreRunning)
            {
                //Mainline Active DB case.
                return DatabaseStateWithRespectToDonorUpdates.Active;
            }

            if (otherDbIsActive && !refreshesAreRunning && !targetIsBeingRefreshed && !multipleRefreshesAreRunning)
            {
                //Mainline Dormant DB case.
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            if (otherDbIsActive && refreshesAreRunning && targetIsBeingRefreshed && !multipleRefreshesAreRunning)
            {
                //Mainline Refreshing DB case.
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }

            if (noActiveDbExists && refreshesAreRunning && !targetIsBeingRefreshed && !multipleRefreshesAreRunning)
            {
                //Secondary Dormant DB case (When initial refresh targets other database.)
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            if (noActiveDbExists && refreshesAreRunning && targetIsBeingRefreshed && !multipleRefreshesAreRunning)
            {
                //Secondary Refreshing DB case (When initial refresh targets this database.)
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }


            // Any other combination of options is weird and unexpected.
            // We shouldn't really ever get to here, but if we do, then let's just leave the messages alone.
            // No value in throwing or logging here - that'll just produce vast amounts of spam in the logs for little benefit. Other processes should be in charge of worrying about this.
            logger.SendTrace($@"Observed a DataRefreshHistory state that was not expected: 
{nameof(targetDbIsActive)}: {targetDbIsActive}.
{nameof(otherDbIsActive)}: {otherDbIsActive}.
{nameof(noActiveDbExists)}: {noActiveDbExists}.
{nameof(refreshesAreRunning)}: {refreshesAreRunning}.
{nameof(targetIsBeingRefreshed)}: {targetIsBeingRefreshed}.
{nameof(multipleRefreshesAreRunning)}: {multipleRefreshesAreRunning}.
", LogLevel.Verbose);

            return DatabaseStateWithRespectToDonorUpdates.Refreshing;
        }



        public IMessageProcessor<SearchableDonorUpdate> ChooseMessagesToProcess(TransientDatabase targetDatabase)
        {
            return targetDatabase switch
            {
                TransientDatabase.DatabaseA => dbAMessageProcessorService,
                TransientDatabase.DatabaseB => dbBMessageProcessorService,
                _ => throw new ArgumentOutOfRangeException(nameof(targetDatabase), targetDatabase, null)
            };
        }

        private async Task ProcessMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> messageBatch, TransientDatabase targetDatabase)
        {
            var converterResults = await searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(messageBatch);

            logger.SendTrace($"{TraceMessagePrefix}: {converterResults.ProcessingResults.Count()} messages retrieved for processing.");

            if (converterResults.ProcessingResults.Any())
            {
                // Currently the donorManagementService acquires a 'hardcoded' connection to the database.
                // This is fine, since this code should only EVER be running against the active Database in the first place.
                // But let's verify that first ... just in case.
                var activeDb = refreshHistoryRepository.GetActiveDatabase();
                if (activeDb != targetDatabase)
                {
                    throw new InvalidOperationException("We shouldn't ever be running this code whilst pointing at a ");
                }
                await donorManagementService.ApplyDonorUpdatesToDatabase(converterResults.ProcessingResults, targetDatabase);
            }
        }

        private Task DiscardMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> messageBatch)
        {
            logger.SendTrace($"{TraceMessagePrefix}: Read and discarded {messageBatch.Count()} messages since the target Database is currently dormant.", LogLevel.Verbose);
            return Task.CompletedTask;
        }
    }
}
