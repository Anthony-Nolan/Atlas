using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Atlas.Utils.Core.ApplicationInsights.ILogger;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorHlaExpanderTests
    {
        private IDonorHlaExpander donorHlaExpander;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();
            logger = Substitute.For<ILogger>();
            donorHlaExpander = new DonorHlaExpander(expandHlaPhenotypeService, logger);
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_ExpandsDonorHla()
        {
            await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
            {
                new DonorInfo
                {
                    HlaNames = new PhenotypeInfo<string>("hla")
                }
            },
                "event-name");

            await expandHlaPhenotypeService.Received().GetPhenotypeOfExpandedHla(Arg.Any<PhenotypeInfo<string>>());
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_HlaDatabaseVersionProvided_ExpandsDonorHlaWithHlaDatabaseVersion()
        {
            const string hlaDatabaseVersion = "version";

            await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
            {
                new DonorInfo
                {
                    HlaNames = new PhenotypeInfo<string>("hla")
                }
            },
                "event-name",
                hlaDatabaseVersion);

            await expandHlaPhenotypeService.Received().GetPhenotypeOfExpandedHla(
                Arg.Any<PhenotypeInfo<string>>(),
                hlaDatabaseVersion);
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_ExpansionSucceeded_ReturnsExpectedDonor()
        {
            const int donorId = 123;

            expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(Arg.Any<PhenotypeInfo<string>>())
                .Returns(new PhenotypeInfo<ExpandedHla>(new ExpandedHla()));

            var result = await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
            {
                new DonorInfo
                {
                    DonorId = donorId,
                    HlaNames = new PhenotypeInfo<string>("hla")
                }
            },
                "event-name");

            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_AnticipatedExpansionFailure_ReturnsFailedDonor()
        {
            const int donorId = 123;

            expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(Arg.Any<PhenotypeInfo<string>>())
                .Throws(new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "error"));

            var result = await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
            {
                new DonorInfo
                {
                    DonorId = donorId,
                    HlaNames = new PhenotypeInfo<string>("hla")
                }
            },
                "event-name");

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == donorId.ToString());
        }

        [Test]
        public void ExpandDonorHlaBatchAsync_AnticipatedExpansionFailure_DoesNotThrowException()
        {
            expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(Arg.Any<Common.Models.PhenotypeInfo<string>>())
                .Throws(new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "error"));

            Assert.DoesNotThrowAsync(async () =>
            {
                await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
                {
                    new DonorInfo
                    {
                        HlaNames = new PhenotypeInfo<string>("hla")
                    }
                },
                    "event-name");
            });
        }

        [Test]
        public void ExpandDonorHlaBatchAsync_UnanticipatedExpansionFailure_ThrowsException()
        {
            expandHlaPhenotypeService
                .GetPhenotypeOfExpandedHla(Arg.Any<Common.Models.PhenotypeInfo<string>>())
                .Throws(new Exception("error"));

            Assert.ThrowsAsync<Exception>(async () =>
                {
                    await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
                    {
                        new DonorInfo
                        {
                            HlaNames = new PhenotypeInfo<string>("hla")
                        }
                    },
                        "event-name");
                }
            );
        }
    }
}
