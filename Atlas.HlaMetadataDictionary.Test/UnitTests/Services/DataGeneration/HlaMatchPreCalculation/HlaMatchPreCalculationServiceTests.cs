using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using AutoFixture.Dsl;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation
{
    /// <summary>
    /// Duplicate WMDA data must not be resolved silently: the pre-calculation either throws, or keeps the first record.
    /// </summary>
    [TestFixture]
    internal class HlaMatchPreCalculationServiceTests
    {
        private const string TypingLocus = "A*";
        private const string AlleleName = "01:01:01:01";

        private IWmdaDataRepository wmdaDataRepository;
        private IHlaMatchPreCalculationService service;

        [SetUp]
        public void SetUp()
        {
            wmdaDataRepository = Substitute.For<IWmdaDataRepository>();
            service = new HlaMatchPreCalculationService(wmdaDataRepository);
        }

        [TestCase(true, Description = "P group")]
        [TestCase(false, Description = "G group")]
        public void GetMatchedHla_WhenAlleleIsInTwoGroupsOfTheLocus_Throws(bool isPGroup)
        {
            var dataset = DatasetWithAllele();
            dataset = isPGroup
                ? dataset.AddPGroup(TypingLocus, "01:01P", AlleleName).AddPGroup(TypingLocus, "01:02P", AlleleName)
                : dataset.AddGGroup(TypingLocus, "01:01:01G", AlleleName).AddGGroup(TypingLocus, "01:02:01G", AlleleName);
            GivenDataset(dataset.Build());

            AssertThrowsOnDuplicate(() => service.GetMatchedHla("version").ToList());
        }

        [Test]
        public void GetMatchedHla_WhenAlleleHasTwoAlleleToSerologyRelationships_Throws()
        {
            var wmdaDataset = DatasetWithAllele().Build();
            wmdaDataset.AlleleToSerologyRelationships = new List<RelDnaSer>
            {
                new(TypingLocus, AlleleName, new[] { new SerologyAssignment("1", Assignment.Unambiguous) }),
                new(TypingLocus, AlleleName, new[] { new SerologyAssignment("1", Assignment.Unambiguous) }),
            };
            GivenDataset(wmdaDataset);

            AssertThrowsOnDuplicate(() => service.GetMatchedHla("version").ToList());
        }

        [Test]
        public void GetMatchedHla_WhenAlleleHasTwoStatuses_UsesTheFirstStatus()
        {
            var wmdaDataset = DatasetWithAllele().Build();
            wmdaDataset.AlleleStatuses = new List<AlleleStatus>
            {
                new(TypingLocus, AlleleName, "Full", "gDNA"),
                new(TypingLocus, AlleleName, "Partial", "cDNA"),
            };
            GivenDataset(wmdaDataset);

            var matchedAllele = service.GetMatchedHla("version").OfType<MatchedAllele>().Single();

            matchedAllele.TypingForHlaMetadata.Status.Should().Be(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna));
        }

        private static IPostprocessComposer<WmdaDataset> DatasetWithAllele() =>
            WmdaDatasetBuilder.New.With(d => d.Alleles, new List<HlaNom> { new(TypingMethod.Molecular, TypingLocus, AlleleName) });

        private void GivenDataset(WmdaDataset dataset) => wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

        /// <remarks>
        /// Alleles are processed in parallel, so the exception of a duplicate may arrive wrapped in an <see cref="AggregateException"/>.
        /// </remarks>
        private static void AssertThrowsOnDuplicate(Action act)
        {
            var exception = act.Should().Throw<Exception>().Which;
            var innermost = exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.Single() : exception;
            innermost.Should().BeOfType<InvalidOperationException>();
        }
    }
}
