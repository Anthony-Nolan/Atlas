using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.Notifications;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorHlaExpanderTests
    {
        private IDonorHlaExpander donorHlaExpander;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            donorHlaExpander = new DonorHlaExpander(expandHlaPhenotypeService, logger, notificationsClient);
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
            });

            await expandHlaPhenotypeService.Received().GetPhenotypeOfExpandedHla(Arg.Any<PhenotypeInfo<string>>());
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
            });

            result.Should().OnlyContain(d => d.DonorId == donorId);
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
                });
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
                    });
                }
            );
        }
    }
}
