using System;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using MatchingAlgorithmFailureInfo = Atlas.SearchTracking.Common.Models.MatchingAlgorithmFailureInfo;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface IMatchingAlgorithmSearchTrackingDispatcher
    {
        Task ProcessInitiation(DateTime initiationTime, DateTime startTime);

        Task ProcessCoreMatchingStarted();

        Task ProcessCoreMatchingEnded();

        Task ProcessCoreScoringOneDonorStarted();

        Task ProcessCoreScoringAllDonorsEnded();

        Task ProcessPersistingResultsStarted();

        Task ProcessPersistingResultsEnded();

        Task ProcessCompleted((string HlaNomenclatureVersion,
            DateTime? ResultsSentTimeUtc, int? NumberOfResults, MatchingAlgorithmFailureInfo FailureInfo,
            MatchingAlgorithmRepeatSearchResultsDetails RepeatSearchResultsDetails, int? NumberOfMatching) eventDetails);
    }

    public class MatchingAlgorithmSearchTrackingDispatcher(
        IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager,
        ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchingAlgorithmSearchTrackingDispatcher
    {

        private bool scoringOneDonorStartedTriggered = false;

        public async Task ProcessInitiation(DateTime initiationTime, DateTime startTime)
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = startTime
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptStartedEvent, SearchTrackingEventType.MatchingAlgorithmAttemptStarted);
        }

        public async Task ProcessCoreMatchingStarted()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted);
        }

        public async Task ProcessCoreMatchingEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded);
        }

        public async Task ProcessCoreScoringOneDonorStarted()
        {
            if (!scoringOneDonorStartedTriggered)
            {
                var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

                var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
                {
                    SearchIdentifier = currentContext.SearchIdentifier,
                    AttemptNumber = currentContext.AttemptNumber,
                    TimeUtc = DateTime.UtcNow
                };

                scoringOneDonorStartedTriggered = true;
                await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                    matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted);
            }
        }

        public async Task ProcessCoreScoringAllDonorsEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded);
        }

        public async Task ProcessPersistingResultsStarted()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded);
        }

        public async Task ProcessCompleted((string HlaNomenclatureVersion,
            DateTime? ResultsSentTimeUtc, int? NumberOfResults, MatchingAlgorithmFailureInfo FailureInfo,
            MatchingAlgorithmRepeatSearchResultsDetails RepeatSearchResultsDetails, int? NumberOfMatching) eventDetails)
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var completedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchIdentifier = currentContext.SearchIdentifier,
                AttemptNumber = currentContext.AttemptNumber,
                CompletionTimeUtc = DateTime.UtcNow,
                HlaNomenclatureVersion = eventDetails.HlaNomenclatureVersion,
                ResultsSent = eventDetails.ResultsSentTimeUtc.HasValue,
                ResultsSentTimeUtc = eventDetails.ResultsSentTimeUtc,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = eventDetails.FailureInfo == null,
                    TotalAttemptsNumber = currentContext.AttemptNumber,
                    NumberOfResults = eventDetails.NumberOfResults,
                    RepeatSearchResultsDetails = eventDetails.RepeatSearchResultsDetails,
                    FailureInfo = eventDetails.FailureInfo
                }
            };
            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(completedEvent, SearchTrackingEventType.MatchingAlgorithmCompleted);
        }
    }
}