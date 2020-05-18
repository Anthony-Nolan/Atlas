﻿using FluentAssertions;
using FluentValidation;
using Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.Common.ServiceBus.Models;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using ILogger = Atlas.Common.ApplicationInsights.ILogger;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorBatchProcessorTests
    {

        private const string DefaultEventName = "event-name";

        #region Function delegates to pass into service under test

        private static string GetDonorIdFromDonorInfoFunc(ServiceBusMessage<SearchableDonorUpdate> donor)
            => donor.DeserializedBody?.DonorId;

        private static FailedDonorInfo GetFailedDonorInfoFunc(ServiceBusMessage<SearchableDonorUpdate> donor)
            => new FailedDonorInfo(donor) { DonorId = GetDonorIdFromDonorInfoFunc(donor) };

        private static Task<DonorAvailabilityUpdate> ProcessDonorFunc(ServiceBusMessage<SearchableDonorUpdate> donor)
            => Task.FromResult(new DonorAvailabilityUpdate
            {
                DonorId = donor.DeserializedBody == null
                        ? default
                        : int.Parse(GetDonorIdFromDonorInfoFunc(donor))
            });

        private class Anticipated : Exception { }
        private static Task<DonorAvailabilityUpdate> Throw<TException>(ServiceBusMessage<SearchableDonorUpdate> donor) where TException : Exception, new() => throw new TException();

        #endregion

        // Arbitrary implementation selected to test base functionality.
        private IDonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdate>, DonorAvailabilityUpdate> donorBatchProcessor;

        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            donorBatchProcessor = new SearchableDonorUpdateConverter(logger);
        }


        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyProcessingResults()
        {
            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>(),
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
                new List<ServiceBusMessage<SearchableDonorUpdate>>(),
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
                new List<ServiceBusMessage<SearchableDonorUpdate>> { new ServiceBusMessage<SearchableDonorUpdate>() },
                Throw<Anticipated>,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.ProcessingResults.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsFailedDonors()
        {
            const string donorId = "donor-id";

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>()
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

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_LogsOneEventPerFailure()
        {
            await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>(),
                    new ServiceBusMessage<SearchableDonorUpdate>()
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
                new List<ServiceBusMessage<SearchableDonorUpdate>> { new ServiceBusMessage<SearchableDonorUpdate>() },
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
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate { DonorId = donorId.ToString() }
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
            const string donorId = "123";

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Exception>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>
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
            const string successfulDonor = "123";
            const string failedDonor = "456";

            await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = successfulDonor }
                    },
                    new ServiceBusMessage<SearchableDonorUpdate>
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
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = successfulDonor.ToString() }
                    },
                    new ServiceBusMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = "456" }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor.ToString()
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
            const string failedDonor = "123";

            var result = await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    new ServiceBusMessage<SearchableDonorUpdate>
                    {
                        DeserializedBody = new SearchableDonorUpdate{ DonorId = "456" }
                    },
                    new ServiceBusMessage<SearchableDonorUpdate>
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

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == failedDonor);
        }

        [Test]
        public void ProcessBatchAsync_AnticipatedFailure_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await donorBatchProcessor.ProcessBatchAsyncWithAnticipatedExceptions<Anticipated>(
                    new List<ServiceBusMessage<SearchableDonorUpdate>> { new ServiceBusMessage<SearchableDonorUpdate>() },
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
                    new List<ServiceBusMessage<SearchableDonorUpdate>> { new ServiceBusMessage<SearchableDonorUpdate>() },
                    Throw<Exception>,
                    GetFailedDonorInfoFunc,
                    DefaultEventName
                );
            }
            );
        }
    }
}