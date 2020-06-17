using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.ExpandAmbiguousPhenotype
{
    [TestFixture]
    internal class CompressedPhenotypeExpanderTests
    {
        private IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;
        private IHlaMetadataDictionary hlaMetadataDictionary;

        private ICompressedPhenotypeExpander compressedPhenotypeExpander;

        [SetUp]
        public void SetUp()
        {
            var hlaMetadataDictionaryFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            hlaMetadataDictionaryFactory.BuildDictionary(default).ReturnsForAnyArgs(hlaMetadataDictionary);

            ambiguousPhenotypeExpander = Substitute.For<IAmbiguousPhenotypeExpander>();

            compressedPhenotypeExpander = new CompressedPhenotypeExpander(hlaMetadataDictionaryFactory, ambiguousPhenotypeExpander);
        }

        [Test]
        public async Task CalculateNumberOfPermutations_ForUnambiguousGenotype_ReturnsOne()
        {
            hlaMetadataDictionary.ConvertHla(default, default, default).ReturnsForAnyArgs(new List<string> {"hla"});

            var numberOfPermutations = await compressedPhenotypeExpander.CalculateNumberOfPermutations(new PhenotypeInfo<string>(), default);

            numberOfPermutations.Should().Be(1);
        }

        // 2 positions at each of 5 loci, means that for equally ambiguous hla we expect n^10 permutations
        [TestCase(2, 1024)]
        [TestCase(3, 59049)]
        [TestCase(5, 9765625)]
        [TestCase(10, 10000000000)]
        public async Task CalculateNumberOfPermutations_ForAmbiguousGenotype_ReturnsCorrectNumberOfPermutations(
            int numberOfAllelesAtEachPosition,
            long expectedNumberOfPermutations)
        {
            hlaMetadataDictionary.ConvertHla(default, default, default).ReturnsForAnyArgs(new string[numberOfAllelesAtEachPosition]);

            var numberOfPermutations = await compressedPhenotypeExpander.CalculateNumberOfPermutations(new PhenotypeInfo<string>(), default);

            numberOfPermutations.Should().Be(expectedNumberOfPermutations);
        }
    }
}