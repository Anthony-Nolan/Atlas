using FluentAssertions;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.Utils.Notifications;
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
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            converter = new SearchableDonorUpdateConverter(logger, notificationsClient);
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

            result.Should().OnlyContain(d => d.DonorId == donorId);
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
    }
}