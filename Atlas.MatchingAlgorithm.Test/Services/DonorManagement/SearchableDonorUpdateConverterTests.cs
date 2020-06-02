using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class SearchableDonorUpdateConverterTests
    {
        private ISearchableDonorUpdateConverter converter;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            converter = new SearchableDonorUpdateConverter(logger);
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_ValidUpdate_ReturnsUpdate()
        {
            const int donorId = 123;

            var result = await converter.ConvertSearchableDonorUpdatesAsync(new List<ServiceBusMessage<SearchableDonorUpdate>>()
            {
                new ServiceBusMessage<SearchableDonorUpdate>
                {
                    LockToken = "token",
                    DeserializedBody = new SearchableDonorUpdate
                    {
                        DonorId = donorId,
                        PublishedDateTime = DateTime.UtcNow,
                        IsAvailableForSearch = false
                    }
                }
            });

            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public void ConvertSearchableDonorUpdatesAsync_InvalidUpdate_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await converter.ConvertSearchableDonorUpdatesAsync(
                    new List<ServiceBusMessage<SearchableDonorUpdate>>
                    {
                        new ServiceBusMessage<SearchableDonorUpdate>()
                    });
            });
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_InvalidUpdate_ReturnsFailedDonorInfo()
        {
            const int donorId = -1;

            var result = await converter.ConvertSearchableDonorUpdatesAsync(new List<ServiceBusMessage<SearchableDonorUpdate>>()
            {
                new ServiceBusMessage<SearchableDonorUpdate>
                {
                    LockToken = "token",
                    DeserializedBody = new SearchableDonorUpdate
                    {
                        DonorId = donorId,
                        IsAvailableForSearch = true,
                        SearchableDonorInformation = null
                    }
                }
            });

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == donorId);
        }
    }
}