using FluentAssertions;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.Utils.Notifications;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorInfoConverterTests
    {
        private IDonorInfoConverter converter;
        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            converter = new DonorInfoConverter(logger, notificationsClient);
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_ValidDonor_ConvertsDonorInfo()
        {
            const int donorId = 123;
            const string hlaName = "hla";

            var result = await converter.ConvertDonorInfoAsync(new List<SearchableDonorInformation>
            {
                new SearchableDonorInformation
                {
                    DonorId = donorId,
                    DonorType = $"{DonorType.Adult}",
                    RegistryCode = $"{RegistryCode.AN}",
                    A_1 = hlaName,
                    A_2 = hlaName,
                    B_1 = hlaName,
                    B_2 = hlaName,
                    DRB1_1 = hlaName,
                    DRB1_2 = hlaName
                }
            });

            result.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public void ConvertSearchableDonorUpdatesAsync_InvalidUpdate_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await converter.ConvertDonorInfoAsync(
                    new List<SearchableDonorInformation>
                    {
                        new SearchableDonorInformation()
                    });
            });
        }
    }
}