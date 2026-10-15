using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.AlleleNames
{
    [TestFixture]
    internal class AlleleNameVariantsExtractorTests
    {
        private const string TypingLocus = "A*";
        private const string AlleleName = "01:01:01:01";

        [Test]
        public void GetAlleleNames_WhenAlleleIsListedTwiceInHlaNom_Throws()
        {
            var wmdaDataRepository = Substitute.For<IWmdaDataRepository>();
            var dataset = WmdaDatasetBuilder.New
                .With(d => d.Alleles, new List<HlaNom>
                {
                    new(TypingMethod.Molecular, TypingLocus, AlleleName),
                    new(TypingMethod.Molecular, TypingLocus, AlleleName, isDeleted: true),
                })
                .Build();
            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);
            var originalAlleleNames = new List<IAlleleNameMetadata> { new AlleleNameMetadata(Locus.A, AlleleName, new[] { AlleleName }) };

            var act = () => new AlleleNameVariantsExtractor(wmdaDataRepository).GetAlleleNames(originalAlleleNames, "version").ToList();

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
