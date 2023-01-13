using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Donors;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorHlaExpanderTests
    {
        private IDonorHlaExpander donorHlaExpander;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IMatchingAlgorithmImportLogger logger;

        [SetUp]
        public void SetUp()
        {
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            hlaMetadataDictionary.GetLocusHlaMatchingMetadata(default, default).ReturnsForAnyArgs(call =>
            {
                call.Arg<LocusInfo<string>>();
                return Task.FromResult(new LocusInfo<INullHandledHlaMatchingMetadata>(default, default));
            });

            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            donorHlaExpander = new DonorHlaExpander(hlaMetadataDictionary, logger);
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_LooksUpHlaData()
        {
            await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
                {
                    new DonorInfo
                    {
                        HlaNames = new PhenotypeInfo<string>("hla")
                    }
                },
                "event-name");


            await hlaMetadataDictionary.Received().GetLocusHlaMatchingMetadata(Arg.Any<Locus>(), Arg.Any<LocusInfo<string>>());
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_HlaNomenclatureVersionProvidedToFactory_ExpandsDonorHlaWithAppropriateDictionary()
        {
            var dictionaryBuilder = new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary);

            var constructedDonorExpander = new DonorHlaExpanderFactory(dictionaryBuilder, Substitute.For<IActiveHlaNomenclatureVersionAccessor>(), Substitute.For<IMatchingAlgorithmImportLogger>()).BuildForSpecifiedHlaNomenclatureVersion(null);
            
            await constructedDonorExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
                {
                    new DonorInfo
                    {
                        HlaNames = new PhenotypeInfo<string>("hla")
                    }
                },
                "event-name");

            await hlaMetadataDictionary.Received().GetLocusHlaMatchingMetadata(
                Arg.Any<Locus>(),
                Arg.Any<LocusInfo<string>>());
        }

        [Test]
        public async Task ExpandDonorHlaBatchAsync_ExpansionSucceeded_ReturnsExpectedDonor()
        {
            const int donorId = 123;

            hlaMetadataDictionary
                .GetLocusHlaMatchingMetadata(default, default)
                .ReturnsForAnyArgs(new LocusInfo<INullHandledHlaMatchingMetadata>(null as INullHandledHlaMatchingMetadata));
                
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

            hlaMetadataDictionary
                .GetLocusHlaMatchingMetadata(default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(Locus.A, "hla", "error"));
                
            var result = await donorHlaExpander.ExpandDonorHlaBatchAsync(new List<DonorInfo>
            {
                new DonorInfo
                {
                    DonorId = donorId,
                    HlaNames = new PhenotypeInfo<string>("hla")
                }
            },
                "event-name");

            result.FailedDonors.Should().OnlyContain(d => d.AtlasDonorId == donorId);
        }

        [Test]
        public void ExpandDonorHlaBatchAsync_AnticipatedExpansionFailure_DoesNotThrowException()
        {
            hlaMetadataDictionary
                .GetLocusHlaMatchingMetadata(default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(Locus.A, "hla", "error"));

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
            hlaMetadataDictionary
                .GetLocusHlaMatchingMetadata(Locus.A, Arg.Any<LocusInfo<string>>())
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
