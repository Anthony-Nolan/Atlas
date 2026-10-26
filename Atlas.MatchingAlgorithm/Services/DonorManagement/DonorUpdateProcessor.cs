using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services.DonorUpdates;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;

namespace Atlas.MatchingAlgorithm.Services.DonorManagement
{
    // Normally we'd put interfaces etc. in their own files.
    // However these interfaces and classes are A) tiny, and B) exist exclusively to serve this file. So they're more useful, convenient and clear if they live here.
    public interface IDonorUpdateMessageProcessorFactory
    {
        /// <summary>
        /// Creates a processor with its own receiver on the subscription of the target database.
        /// The caller must dispose it as soon as it is done, so the receiver cannot keep link credit or lock messages that nobody will process.
        /// </summary>
        IMessageProcessor<SearchableDonorUpdate> CreateMessageProcessor(TransientDatabase targetDatabase);
    }

    public class DonorUpdateMessageProcessorFactory : IDonorUpdateMessageProcessorFactory
    {
        private readonly IMessageReceiverFactory messageReceiverFactory;
        private readonly DonorManagementSettings settings;

        public DonorUpdateMessageProcessorFactory(IMessageReceiverFactory messageReceiverFactory, DonorManagementSettings settings)
        {
            this.messageReceiverFactory = messageReceiverFactory;
            this.settings = settings;
        }

        public IMessageProcessor<SearchableDonorUpdate> CreateMessageProcessor(TransientDatabase targetDatabase)
        {
            var subscriptionName = targetDatabase switch
            {
                TransientDatabase.DatabaseA => settings.SubscriptionForDbA,
                TransientDatabase.DatabaseB => settings.SubscriptionForDbB,
                _ => throw new ArgumentOutOfRangeException(nameof(targetDatabase), targetDatabase, null)
            };

            var messageReceiver = new ServiceBusMessageReceiver<SearchableDonorUpdate>(messageReceiverFactory, settings.Topic, subscriptionName);
            return new MessageProcessor<SearchableDonorUpdate>(messageReceiver);
        }
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
        Task ApplyDifferentialDonorUpdatesDuringRefresh(TransientDatabase targetDatabase, string hlaNomenclatureVersionFromRefresh);
        Task ProcessDeadLetterDifferentialDonorUpdates(SearchableDonorUpdate[] searchableDonorUpdates);
    }

    public class DonorUpdateProcessor : IDonorUpdateProcessor
    {
        private const string TraceMessagePrefix = nameof(ProcessDifferentialDonorUpdates);

        private readonly IDonorUpdateMessageProcessorFactory messageProcessorFactory;
        private readonly IDataRefreshHistoryRepository refreshHistoryRepository;
        private readonly IDonorManagementService donorManagementService;
        private readonly ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private readonly IActiveHlaNomenclatureVersionAccessor activeHlaNomenclatureVersionAccessor;
        private readonly DonorManagementSettings settings;
        private readonly int batchSize;
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly MatchingAlgorithmImportLoggingContext loggingContext;
        private readonly IDonorReader donorReader;
        private readonly IDonorUpdatesSaver donorUpdatesSaver;

        public DonorUpdateProcessor(
            IDonorUpdateMessageProcessorFactory messageProcessorFactory,
            IDataRefreshHistoryRepository refreshHistoryRepository,
            IDonorManagementService donorManagementService,
            ISearchableDonorUpdateConverter searchableDonorUpdateConverter,
            IActiveHlaNomenclatureVersionAccessor activeHlaNomenclatureVersionAccessor,
            DonorManagementSettings settings,
            IMatchingAlgorithmImportLogger logger,
            MatchingAlgorithmImportLoggingContext loggingContext,
            IDonorReader donorReader,
            IDonorUpdatesSaver donorUpdatesSaver)
        {
            this.messageProcessorFactory = messageProcessorFactory;
            this.refreshHistoryRepository = refreshHistoryRepository;
            this.donorManagementService = donorManagementService;
            this.searchableDonorUpdateConverter = searchableDonorUpdateConverter;
            this.activeHlaNomenclatureVersionAccessor = activeHlaNomenclatureVersionAccessor;
            this.settings = settings;
            this.batchSize = settings.BatchSize;
            this.logger = logger;
            this.loggingContext = loggingContext;
            this.donorReader = donorReader;
            this.donorUpdatesSaver = donorUpdatesSaver;
        }

        /// <remarks>
        /// It is intended that this method will be invoked by the Data Refresh process.
        /// </remarks>
        public async Task ApplyDifferentialDonorUpdatesDuringRefresh(TransientDatabase targetDatabase, string hlaNomenclatureVersionFromRefresh)
        {
            var targetDatabaseState = DetermineDatabaseState(targetDatabase);
            if (targetDatabaseState != DatabaseStateWithRespectToDonorUpdates.Refreshing)
            {
                throw new InvalidOperationException($"The method {nameof(ApplyDifferentialDonorUpdatesDuringRefresh)} was called an analysis of the DataRefreshHistory table implies that '{targetDatabase.GetStringValue()}' is not currently Refreshing");
            }

            // The Data Refresh process never receives from this subscription again, so the receiver must be closed with the stage.
            await ProcessMessagesWithOwnReceiver(
                targetDatabase,
                async batch => await ProcessMessages(batch, targetDatabase, hlaNomenclatureVersionFromRefresh),
                batchSize);
        }

        /// <remarks>
        /// It is intended that this method will be invoked by a cron timer, against both databases including when a DataRefresh is taking place.
        /// </remarks>
        public async Task ProcessDifferentialDonorUpdates(TransientDatabase targetDatabase)
        {
            var targetDatabaseState = DetermineDatabaseState(targetDatabase);

            switch (targetDatabaseState)
            {
                case DatabaseStateWithRespectToDonorUpdates.Active:
                    var activeHlaVersion = activeHlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
                    await ProcessMessagesWithOwnReceiver(
                        targetDatabase,
                        async batch => await ProcessMessages(batch, targetDatabase, activeHlaVersion),
                        batchSize);
                    return;

                case DatabaseStateWithRespectToDonorUpdates.Dormant:
                    await ProcessMessagesWithOwnReceiver(
                        targetDatabase,
                        async batch => await DiscardMessages(batch),
                        batchSize * 10);
                    return;

                case DatabaseStateWithRespectToDonorUpdates.Refreshing:
                    // Do nothing!
                    // We want the messages to be left in the queue.
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task ProcessDeadLetterDifferentialDonorUpdates(SearchableDonorUpdate[] searchableDonorUpdates)
        {
            var existingDonors = await donorReader.GetDonors(searchableDonorUpdates.Select(d => d.DonorId));
            foreach (var searchableDonorUpdate in searchableDonorUpdates)
            {
                searchableDonorUpdate.PublishedDateTime = DateTimeOffset.UtcNow;
                searchableDonorUpdate.IsAvailableForSearch = existingDonors.ContainsKey(searchableDonorUpdate.DonorId);

                if (searchableDonorUpdate.IsAvailableForSearch)
                {
                    searchableDonorUpdate.SearchableDonorInformation = existingDonors[searchableDonorUpdate.DonorId].MapImportDonorToMatchingUpdateDonor();
                }
            }
            await donorUpdatesSaver.Save(searchableDonorUpdates);
        }

        private DatabaseStateWithRespectToDonorUpdates DetermineDatabaseState(TransientDatabase targetDatabase)
        {
            var activeDb = refreshHistoryRepository.GetActiveDatabase();
            var refreshJobs = refreshHistoryRepository.GetIncompleteRefreshJobs().ToList();
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

        /// <summary>
        /// Processes a batch with a receiver that exists only for this call, and closes it afterwards, also when processing fails.
        /// </summary>
        private async Task ProcessMessagesWithOwnReceiver(
            TransientDatabase targetDatabase,
            Func<IEnumerable<DeserializedMessage<SearchableDonorUpdate>>, Task> processMessagesFuncAsync,
            int messageBatchSize)
        {
            var messageProcessor = messageProcessorFactory.CreateMessageProcessor(targetDatabase);
            try
            {
                await messageProcessor.ProcessAllMessagesInBatches_Async(processMessagesFuncAsync, messageBatchSize);
            }
            finally
            {
                await CloseReceiver(messageProcessor, targetDatabase);
            }
        }

        /// <remarks>
        /// A failure to close must not fail a run whose messages were processed, nor hide the error of a run that failed:
        /// by now each message has been completed or abandoned, or comes back when its lock expires.
        /// </remarks>
        private async Task CloseReceiver(IMessageProcessor<SearchableDonorUpdate> messageProcessor, TransientDatabase targetDatabase)
        {
            try
            {
                await messageProcessor.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.SendTrace(
                    $"{TraceMessagePrefix}: Failed to close the donor update receiver for '{targetDatabase.GetStringValue()}': {ex.Message}",
                    LogLevel.Warn);
            }
        }

        private async Task ProcessMessages(IEnumerable<DeserializedMessage<SearchableDonorUpdate>> messageBatch, TransientDatabase targetDatabase, string targetHlaNomenclatureVersion)
        {
            var converterResults = await searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(messageBatch);

            logger.SendTrace($"{TraceMessagePrefix}: {converterResults.ProcessingResults.Count()} messages retrieved for processing.");
            loggingContext.HlaNomenclatureVersion = targetHlaNomenclatureVersion;
            
            if (converterResults.ProcessingResults.Any())
            {
                await donorManagementService.ApplyDonorUpdatesToDatabase(
                    converterResults.ProcessingResults,
                    targetDatabase,
                    targetHlaNomenclatureVersion,
                    settings.OngoingDifferentialDonorUpdatesShouldBeFullyTransactional);
            }
        }

        private Task DiscardMessages(IEnumerable<DeserializedMessage<SearchableDonorUpdate>> messageBatch)
        {
            logger.SendTrace($"{TraceMessagePrefix}: Read and discarded {messageBatch.Count()} messages since the target Database is currently dormant.", LogLevel.Verbose);
            return Task.CompletedTask;
        }
    }
}
