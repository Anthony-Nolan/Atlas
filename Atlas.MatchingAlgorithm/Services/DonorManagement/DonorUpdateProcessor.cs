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
                        batchSize, prefetchCount:
                        batchSize * 2);
                    return;

                case DatabaseStateWithRespectToDonorUpdates.Dormant:
                    await messageProcessorService.ProcessMessageBatchAsync(
                        async batch => await DiscardMessages(batch),
                        batchSize * 10, prefetchCount:
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

        //QQ @Reviewer. I can't decide which version of this method I prefer. Please decide for me.
        private DatabaseStateWithRespectToDonorUpdates DetermineDatabaseState(TransientDatabase targetDatabase)
        {
            var activeDb = refreshHistoryRepository.GetActiveDatabase();
            var refreshJobs = refreshHistoryRepository.GetInProgressJobs().ToList();
            var refreshesAreRunning = refreshJobs.Any();
            var multipleRefreshesAreRunning = refreshJobs.Count > 1;
            var dbBeingRefreshed = refreshJobs.FirstOrDefault()?.Database?.ParseToEnum<TransientDatabase>();

            if (activeDb == targetDatabase /* && Don't care whether refresh is running*/ && dbBeingRefreshed != targetDatabase && !multipleRefreshesAreRunning)
            {
                //Mainline Active DB case.
                return DatabaseStateWithRespectToDonorUpdates.Active;
            }

            if (activeDb != null && activeDb != targetDatabase && !refreshesAreRunning && dbBeingRefreshed != targetDatabase && !multipleRefreshesAreRunning)
            {
                //Mainline Dormant DB case.
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            if (activeDb != null && activeDb != targetDatabase && refreshesAreRunning && dbBeingRefreshed == targetDatabase && !multipleRefreshesAreRunning)
            {
                //Mainline Refreshing DB case.
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }

            if (activeDb == null && refreshesAreRunning && dbBeingRefreshed != targetDatabase && !multipleRefreshesAreRunning)
            {
                //Secondary Dormant DB case (When initial refresh targets other database.)
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            if (activeDb == null && refreshesAreRunning && dbBeingRefreshed == targetDatabase && !multipleRefreshesAreRunning)
            {
                //Secondary Refreshing DB case (When initial refresh targets this database.)
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }


            // Any other combination of options is weird and unexpected.
            // We shouldn't really ever get to here, but if we do, then let's just leave the messages alone.
            // No value in throwing or logging here - that'll just produce vast amounts of spam in the logs for little benefit. Other processes should be in charge of worrying about this.
            return DatabaseStateWithRespectToDonorUpdates.Refreshing;
        }


        //QQ @Reviewer. I can't decide which version of this method I prefer. Please decide for me.
        private DatabaseStateWithRespectToDonorUpdates DetermineDatabaseState2(TransientDatabase targetDatabase)
        {
            var activeDb = refreshHistoryRepository.GetActiveDatabase();
            var refreshJobs = refreshHistoryRepository.GetInProgressJobs().ToList();
            var refreshesAreRunning = refreshJobs.Any();
            var multipleRefreshesAreRunning = refreshJobs.Count > 1;
            var dbBeingRefreshed = refreshJobs.FirstOrDefault()?.Database.ParseToEnum<TransientDatabase>();

            if (multipleRefreshesAreRunning)
            {
                // This should NEVER be true, so let's just leave the messages alone until things make more sense.
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }

            if (activeDb == null)
            {
                if (!refreshesAreRunning)
                {
                    // System setup state?
                    // Again just leave the messages until things make more sense.
                    return DatabaseStateWithRespectToDonorUpdates.Refreshing;
                }

                if (dbBeingRefreshed == targetDatabase)
                {
                    // First time refresh, and we're the subject, so hold the messages until they're applied.
                    return DatabaseStateWithRespectToDonorUpdates.Refreshing;
                }

                // The OTHER database is the subject of the first-time refresh.
                // So we can (and SHOULD!) discard these messages.
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            //These are the mainline cases...
            if (activeDb == targetDatabase)
            {
                if (refreshesAreRunning && dbBeingRefreshed == targetDatabase)
                {
                    //Surprised by this - we'd expect the refreshing DB to be the OTHER one, but sure, whatever.
                    return DatabaseStateWithRespectToDonorUpdates.Refreshing;
                }

                //Mainline Active DB case.
                return DatabaseStateWithRespectToDonorUpdates.Active;
            }

            //By the time we're here, we know: "ActiveDb" is defined, but not us.
            if (!refreshesAreRunning)
            {
                // Mainline Dormant DB case
                return DatabaseStateWithRespectToDonorUpdates.Dormant;
            }

            //By the time we're here, we know: "ActiveDb" is defined, but not us, and exactly 1 refresh IS running.
            if (dbBeingRefreshed == targetDatabase)
            {
                //Mainline Refreshing DB case.
                return DatabaseStateWithRespectToDonorUpdates.Refreshing;
            }

            // ... What? Urmmm ... another weird state that we don't understand. So again, just leave the messages where they are.
            return DatabaseStateWithRespectToDonorUpdates.Refreshing; //QQ
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
                // Currently the donorManagmentService acquires a 'hardcoded' connection to the database.
                // This is fine, since this code should only EVER be running against the active Database in the first place.
                // But let's verify that first ... just in case.
                var activeDb = refreshHistoryRepository.GetActiveDatabase();
                if (activeDb != targetDatabase)
                {
                    throw new InvalidOperationException("We shouldn't ever be running this code whilst pointing at a ");
                }
                await donorManagementService.ManageDonorBatchByAvailability(converterResults.ProcessingResults);
            }
        }

        private Task DiscardMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> messageBatch)
        {
            logger.SendTrace($"{TraceMessagePrefix}: Read and discarded {messageBatch.Count()} messages since the target Database is currently dormant.");
            return Task.CompletedTask;
        }
    }
}
