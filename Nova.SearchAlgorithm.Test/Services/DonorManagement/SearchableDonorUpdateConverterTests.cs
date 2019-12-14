using FluentAssertions;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.Utils.ServiceBus.Models;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Services.DonorManagement
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

            var result = await converter.ConvertSearchableDonorUpdatesAsync(new List<ServiceBusMessage<SearchableDonorUpdateModel>>()
            {
                new ServiceBusMessage<SearchableDonorUpdateModel>
                {
                    LockToken = "token",
                    DeserializedBody = new SearchableDonorUpdateModel
                    {
                        DonorId = donorId.ToString(),
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
                    new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                    {
                        new ServiceBusMessage<SearchableDonorUpdateModel>()
                    });
            });
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_InvalidUpdate_ReturnsFailedDonorInfo()
        {
            const string donorId = "donor-id";

            var result = await converter.ConvertSearchableDonorUpdatesAsync(new List<ServiceBusMessage<SearchableDonorUpdateModel>>()
            {
                new ServiceBusMessage<SearchableDonorUpdateModel>
                {
                    LockToken = "token",
                    DeserializedBody = new SearchableDonorUpdateModel
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