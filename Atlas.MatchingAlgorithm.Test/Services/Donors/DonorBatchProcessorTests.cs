using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorBatchProcessorTests
    {

        private const string DefaultEventName = "event-name";

        #region Function delegates to pass into service under test

        private static int? GetDonorIdFromDonorInfoFunc(DeserializedMessage<SearchableDonorUpdate> donor)
            => donor.DeserializedBody?.DonorId;

        private static FailedDonorInfo GetFailedDonorInfoFunc(DeserializedMessage<SearchableDonorUpdate> donor)
        {
            var donorIdFromDonorInfoFunc = GetDonorIdFromDonorInfoFunc(donor);
            return new FailedDonorInfo(donor) {AtlasDonorId = donorIdFromDonorInfoFunc ?? default};
        }

        private static Task<DonorAvailabilityUpdate> ProcessDonorFunc(DeserializedMessage<SearchableDonorUpdate> donor)
        {
            var donorIdFromDonorInfoFunc = GetDonorIdFromDonorInfoFunc(donor);
            return Task.FromResult(new DonorAvailabilityUpdate
            {
                DonorId = donorIdFromDonorInfoFunc ?? 0
            });
        }

        private class Anticipated : Exception { }
        private static Task<DonorAvailabilityUpdate> Throw<TException>(DeserializedMessage<SearchableDonorUpdate> donor) where TException : Exception, new() => throw new TException();

        #endregion

        // Arbitrary implementation selected to test base functionality.
        private IDonorBatchProcessor<DeserializedMessage<SearchableDonorUpdate>, DonorAvailabilityUpdate> donorBatchProcessor;

        private IMatchingAlgorithmImportLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            donorBatchProcessor = new SearchableDonorUpdateConverter(logger);
        }


        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyProcessingResults()
        {
            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<DeserializedMessage<SearchableDonorUpdate>>(),
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.ProcessingResults.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyFailedDonors()
        {
            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<DeserializedMessage<SearchableDonorUpdate>>(),
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsEmptyProcessingResults()
        {
            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>> { new DeserializedMessage<SearchableDonorUpdate>() },
                Throw<Anticipated>,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.ProcessingResults.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsFailedDonors()
        {
            const int donorId = 123;

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>()
                    {
                        DeserializedBody = new SearchableDonorUpdate
                        {
                            DonorId = donorId
                        }
                    }
                },
                Throw<Anticipated>,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().OnlyContain(d => d.AtlasDonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_LogsOneEventPerFailure()
        {
            await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>(),
                    new DeserializedMessage<SearchableDonorUpdate>()
                },
                Throw<Anticipated>,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.Received(2).SendEvent(Arg.Any<DonorInfoGenericFailureEventModel<Anticipated>>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotLogEvent()
        {
            await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<DeserializedMessage<SearchableDonorUpdate>> { new DeserializedMessage<SearchableDonorUpdate>() },
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.DidNotReceive().SendEvent(Arg.Any<EventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsExpectedProcessingResults()
        {
            const int donorId = 123;

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate { DonorId = donorId }
                    }
                },
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            // The result will depend on the processDonorInfoFunc and the return type defined in the concrete class
            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsEmptyFailedDonors()
        {
            const int donorId = 123;

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate
                        {
                            DonorId = donorId
                        }
                    }
                },
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_LogsOneEvent()
        {
            const int successfulDonor = 123;
            const int failedDonor = 456;

            await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = successfulDonor }
                    },
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = failedDonor }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor
                    ? await ProcessDonorFunc(d)
                    : await Throw<Anticipated>(d),
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.Received(1).SendEvent(Arg.Any<DonorInfoGenericFailureEventModel<Anticipated>>());
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsProcessingResult()
        {
            const int successfulDonor = 123;

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = successfulDonor }
                    },
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = 456 }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor
                    ? await ProcessDonorFunc(d)
                    : await Throw<Anticipated>(d),
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            // The result will depend on the return type defined in the concrete class
            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == successfulDonor);
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsFailedDonor()
        {
            const int failedDonor = 123;

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<DeserializedMessage<SearchableDonorUpdate>>
                {
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = 456 }
                    },
                    new DeserializedMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = failedDonor }
                    }
                },
                async d => d.DeserializedBody.DonorId == failedDonor
                    ? await Throw<Anticipated>(d)
                    : await ProcessDonorFunc(d),
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().OnlyContain(d => d.AtlasDonorId == failedDonor);
        }

        [Test]
        public void ProcessBatchAsync_AnticipatedFailure_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                    new List<DeserializedMessage<SearchableDonorUpdate>> { new DeserializedMessage<SearchableDonorUpdate>() },
                    Throw<Anticipated>,
                    GetFailedDonorInfoFunc,
                    DefaultEventName
                );
            }
            );
        }

        [Test]
        public void ProcessBatchAsync_UnanticipatedFailure_ThrowsException()
        {
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                    new List<DeserializedMessage<SearchableDonorUpdate>> { new DeserializedMessage<SearchableDonorUpdate>() },
                    Throw<Exception>,
                    GetFailedDonorInfoFunc,
                    DefaultEventName
                );
            }
            );
        }
    }
}