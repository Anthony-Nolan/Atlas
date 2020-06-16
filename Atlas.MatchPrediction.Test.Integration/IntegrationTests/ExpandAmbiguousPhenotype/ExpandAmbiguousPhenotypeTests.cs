using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using FluentAssertions;
using LochNessBuilder;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.ExpandAmbiguousPhenotype
{
    [TestFixture]
    public class ExpandAmbiguousPhenotypeTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;

        private const string HlaNomenclatureVersion = "3330";

        private const string AlleleStringOfNames = "01:01/01:02";
        private const string AlleleStringOfSubtypes = "01:01/02";

        private const string ThreeFieldAllele = "01:01:01";
        private const string FourFieldAllele = "01:01:01:01";

        private const string A1 = "01:01";
        private const string A2 = "01:02";
        private const string B1 = "02:01";
        private const string B2 = "02:02";
        private const string C1 = "03:01";
        private const string C2 = "03:02";
        private const string Dqb11 = "04:01";
        private const string Dqb12 = "04:02";
        private const string Drb11 = "05:01";
        private const string Drb12 = "05:02";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            compressedPhenotypeExpander =
                DependencyInjection.DependencyInjection.Provider.GetService<ICompressedPhenotypeExpander>();
        }

        [TestCase(A1)]
        [TestCase(ThreeFieldAllele)]
        [TestCase(FourFieldAllele)]
        public async Task ExpandCompressedPhenotype_WhenNoAmbiguousAlleles_ReturnsExpectedGenotype(string allele)
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = allele, Position2 = A2})
                .Build();

            var expectedGenotypes = NewPhenotypeInfo.Build();

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Single().Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenAlleleStringOfNamesPresent_ReturnsExpectedGenotypes()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = AlleleStringOfNames, Position2 = AlleleStringOfNames})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A1}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A1}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A2}).Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenAlleleStringOfSubtypesPresent_ReturnsExpectedGenotypes()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = AlleleStringOfSubtypes, Position2 = AlleleStringOfSubtypes})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A1}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A1}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2}).Build(),
                NewPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A2}).Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenMixOfAmbiguousAllelesPresent_ReturnsExpectedGenotypes()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = AlleleStringOfSubtypes, Position2 = AlleleStringOfNames})
                .With(d => d.B, new LocusInfo<string> {Position1 = ThreeFieldAllele, Position2 = FourFieldAllele})
                .Build();


            NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = AlleleStringOfSubtypes, Position2 = AlleleStringOfNames})
                .With(d => d.B, new LocusInfo<string> {Position1 = ThreeFieldAllele, Position2 = FourFieldAllele})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewPhenotypeInfo
                    .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A1})
                    .With(d => d.B, new LocusInfo<string> {Position1 = A1, Position2 = A1})
                    .Build(),
                NewPhenotypeInfo
                    .With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A1})
                    .With(d => d.B, new LocusInfo<string> {Position1 = A1, Position2 = A1})
                    .Build(),
                NewPhenotypeInfo
                    .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
                    .With(d => d.B, new LocusInfo<string> {Position1 = A1, Position2 = A1})
                    .Build(),
                NewPhenotypeInfo
                    .With(d => d.A, new LocusInfo<string> {Position1 = A2, Position2 = A2})
                    .With(d => d.B, new LocusInfo<string> {Position1 = A1, Position2 = A1})
                    .Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        // TODO: ATLAS-370 - Create tests for g-group alleles

        // TODO: ATLAS-369 - Create tests for p-group alleles

        // TODO: ATLAS-368 - Create tests for serology alleles

        // TODO: ATLAS-367 - Create tests for XX code alleles

        // TODO: ATLAS-407 - Create tests for NMDP alleles

        private static Builder<PhenotypeInfo<string>> NewPhenotypeInfo => Builder<PhenotypeInfo<string>>.New
            .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
            .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
            .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
            .With(d => d.Dpb1, new LocusInfo<string> {Position1 = null, Position2 = null})
            .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
            .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12});
    }
}