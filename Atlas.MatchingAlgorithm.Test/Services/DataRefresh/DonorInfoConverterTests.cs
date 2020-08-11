using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorInfoConverterTests
    {
        private IDonorInfoConverter converter;
        private IMatchingAlgorithmImportLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            converter = new DonorInfoConverter(logger);
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
                    DonorType = DonorType.Adult,
                    A_1 = hlaName,
                    A_2 = hlaName,
                    B_1 = hlaName,
                    B_2 = hlaName,
                    DRB1_1 = hlaName,
                    DRB1_2 = hlaName
                }
            },
                "event-name");

            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == donorId);
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
                    },
                    "event-name");
            });
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_InvalidUpdate_ReturnsFailedDonorInfo()
        {
            const int donorId = 123;

            var result = await converter.ConvertDonorInfoAsync(new List<SearchableDonorInformation>
            {
                new SearchableDonorInformation
                {
                    DonorId = donorId
                }
            },
                "event-name");

            result.FailedDonors.Should().OnlyContain(d => d.AtlasDonorId == donorId);
        }
    }
}