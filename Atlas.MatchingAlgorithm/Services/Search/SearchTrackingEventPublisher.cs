using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Models;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using MatchingAlgorithmFailureInfo = Atlas.SearchTracking.Common.Models.MatchingAlgorithmFailureInfo;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchTrackingEventPublisher
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

    public class SearchTrackingEventPublisher(
        MatchingAlgorithmSearchTrackingContext matchingAlgorithmSearchTrackingContext,
        ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : ISearchTrackingEventPublisher
    {

        private bool scoringOneDonorStartedTriggered = false;

        public async Task ProcessInitiation(DateTime initiationTime, DateTime startTime)
        {
            var matchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = startTime
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptStartedEvent, SearchTrackingEventType.MatchingAlgorithmAttemptStarted);
        }

        public async Task ProcessCoreMatchingStarted()
        {
            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted);
        }

        public async Task ProcessCoreMatchingEnded()
        {
            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded);
        }

        public async Task ProcessCoreScoringOneDonorStarted()
        {
            if (!scoringOneDonorStartedTriggered)
            {
                var currentContext = matchingAlgorithmSearchTrackingContext;

                var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
                {
                    SearchIdentifier = currentContext.SearchIdentifier,
                    OriginalSearchIdentifier = currentContext.OriginalSearchIdentifier,
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
            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded);
        }

        public async Task ProcessPersistingResultsStarted()
        {
            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded()
        {
            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded);
        }

        public async Task ProcessCompleted((string HlaNomenclatureVersion,
            DateTime? ResultsSentTimeUtc, int? NumberOfResults, MatchingAlgorithmFailureInfo FailureInfo,
            MatchingAlgorithmRepeatSearchResultsDetails RepeatSearchResultsDetails, int? NumberOfMatching) eventDetails)
        {
            var completedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchIdentifier = matchingAlgorithmSearchTrackingContext.SearchIdentifier,
                OriginalSearchIdentifier = matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier,
                AttemptNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                CompletionTimeUtc = DateTime.UtcNow,
                HlaNomenclatureVersion = eventDetails.HlaNomenclatureVersion,
                ResultsSent = eventDetails.ResultsSentTimeUtc.HasValue,
                ResultsSentTimeUtc = eventDetails.ResultsSentTimeUtc,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = eventDetails.FailureInfo == null,
                    TotalAttemptsNumber = matchingAlgorithmSearchTrackingContext.AttemptNumber,
                    NumberOfResults = eventDetails.NumberOfResults,
                    RepeatSearchResultsDetails = eventDetails.RepeatSearchResultsDetails,
                    FailureInfo = eventDetails.FailureInfo
                }
            };
            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(completedEvent, SearchTrackingEventType.MatchingAlgorithmCompleted);
        }
    }
}