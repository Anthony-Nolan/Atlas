using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorInfoConverterTests
    {
        private IDonorInfoConverter converter;
        private IMatchingAlgorithmImportLogger logger;
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
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

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_EmitsValidationAndMappingDurationsOncePerBatch()
        {
            await converter.ConvertDonorInfoAsync(fixture.CreateMany<SearchableDonorInformation>(5), "event-name");

            // Once per batch, NOT once per donor: at refresh scale a metric call per donor per half would be tens of
            // millions of calls and enough allocation to distort the GC numbers the runtime sampler is measuring.
            logger.Received(1).SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Arg.Any<double>(),
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_DonorValidation)));

            logger.Received(1).SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Arg.Any<double>(),
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_DonorMapping)));
        }

        [Test]
        public async Task ConvertSearchableDonorUpdatesAsync_WhenAllDonorsFailValidation_StillAttributesTheValidationTime()
        {
            // Every donor throws here, so mapping never runs. The validation half must still be reported, else the
            // two halves would not reconcile against DonorInfoConversion on any run that had failures.
            var invalidDonors = fixture.Build<SearchableDonorInformation>().OmitAutoProperties().CreateMany(3);

            await converter.ConvertDonorInfoAsync(invalidDonors, "event-name");

            logger.Received(1).SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Arg.Is<double>(ms => ms >= 0),
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_DonorValidation)));
        }

        private static bool IsOperation(Dictionary<string, string> dimensions, string operation) =>
            dimensions[DataRefreshMetrics.OperationDimension] == operation;
    }
}