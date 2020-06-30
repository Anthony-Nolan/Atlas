using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.ExpandAmbiguousPhenotype
{
    [TestFixture]
    public class ExpandAmbiguousPhenotypeTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;

        private const string HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;

        private const string A1 = "02:09";
        private const string A2 = "02:66";
        private const string B1 = "08:182";
        private const string B2 = "15:146";
        private const string C1 = "01:03";
        private const string C2 = "03:05";
        private const string Dqb11 = "03:09";
        private const string Dqb12 = "02:04";
        private const string Drb11 = "03:124";
        private const string Drb12 = "11:129";

        private const string A1GGroup = "02:01:01G";
        private const string A2GGroup = "02:01:01G";
        private const string B1GGroup = "08:01:01G";
        private const string B2GGroup = "15:01:01G";
        private const string C1GGroup = "01:03:01G";
        private const string C2GGroup = "03:05:01G";
        private const string Dqb11GGroup = "03:01:01G";
        private const string Dqb12GGroup = "02:01:01G";
        private const string Drb11GGroup = "03:01:01G";
        private const string Drb12GGroup = "11:06:01G";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            compressedPhenotypeExpander =
                DependencyInjection.DependencyInjection.Provider.GetService<ICompressedPhenotypeExpander>();
        }

        [TestCase(A1)]
        [TestCase("02:09:01")]
        [TestCase("02:09:01:01")]
        public async Task ExpandCompressedPhenotype_WhenNoAmbiguousAlleles_ReturnsExpectedGenotype(string allele)
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = allele, Position2 = A2})
                .Build();

            var expectedGenotypes = NewGGroupPhenotypeInfo.Build();

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Single().Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenAlleleStringOfNamesPresent_ReturnsExpectedGenotypes()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = "02:09/02:04", Position2 = "02:09/02:04"})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1GGroup, Position2 = A1GGroup}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = "02:04:01G", Position2 = A1GGroup}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1GGroup, Position2 = "02:04:01G"}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = "02:04:01G", Position2 = "02:04:01G"}).Build()
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
                .With(d => d.A, new LocusInfo<string> {Position1 = "02:09/04", Position2 = "02:09/04"})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1GGroup, Position2 = A1GGroup}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = "02:04:01G", Position2 = A1GGroup}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = A1GGroup, Position2 = "02:04:01G"}).Build(),
                NewGGroupPhenotypeInfo.With(d => d.A, new LocusInfo<string> {Position1 = "02:04:01G", Position2 = "02:04:01G"}).Build()
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
                .With(d => d.A, new LocusInfo<string> {Position1 = "02:09:01", Position2 = "02:09:01:01"})
                .With(d => d.B, new LocusInfo<string> {Position1 = "15:19/146", Position2 = B2})
                .With(d => d.C, new LocusInfo<string> {Position1 = "01:03/01:44", Position2 = C2})
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                NewGGroupPhenotypeInfo
                    .With(d => d.B, new LocusInfo<string> {Position1 = "15:12:01G", Position2 = B2GGroup})
                    .With(d => d.C, new LocusInfo<string> {Position1 = "01:03:01G", Position2 = C2GGroup})
                    .Build(),
                NewGGroupPhenotypeInfo
                    .With(d => d.B, new LocusInfo<string> {Position1 = "15:12:01G", Position2 = B2GGroup})
                    .With(d => d.C, new LocusInfo<string> {Position1 = "01:02:01G", Position2 = C2GGroup})
                    .Build(),
                NewGGroupPhenotypeInfo
                    .With(d => d.B, new LocusInfo<string> {Position1 = "15:01:01G", Position2 = B2GGroup})
                    .With(d => d.C, new LocusInfo<string> {Position1 = "01:03:01G", Position2 = C2GGroup})
                    .Build(),
                NewGGroupPhenotypeInfo
                    .With(d => d.B, new LocusInfo<string> {Position1 = "15:01:01G", Position2 = B2GGroup})
                    .With(d => d.C, new LocusInfo<string> {Position1 = "01:02:01G", Position2 = C2GGroup})
                    .Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenMacPresent_ExpandsMac()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv
            const string mac = "01:AC";

            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = mac, Position2 = A2})
                .Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion);

            // The two 2-field alleles represented by the MAC cover 86 G-Groups
            genotypes.Count().Should().Be(86);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenXXCodePresent_ExpandsXXCode()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.A, new LocusInfo<string> {Position1 = "01:XX", Position2 = A2})
                .Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion);

            genotypes.Count().Should().Be(303);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenSerologyPresent_ExpandsSerology()
        {
            var phenotype = NewPhenotypeInfo
                .With(d => d.B, new LocusInfo<string> { Position1 = "82", Position2 = B2 })
                .Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion);

            genotypes.Count().Should().Be(4);
        }
        // TODO: ATLAS-370 - Create tests for g-group alleles

        // TODO: ATLAS-369 - Create tests for p-group alleles

        private static Builder<PhenotypeInfo<string>> NewPhenotypeInfo => Builder<PhenotypeInfo<string>>.New
            .With(d => d.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
            .With(d => d.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
            .With(d => d.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
            .With(d => d.Dpb1, new LocusInfo<string> {Position1 = null, Position2 = null})
            .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
            .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12});

        private static Builder<PhenotypeInfo<string>> NewGGroupPhenotypeInfo => Builder<PhenotypeInfo<string>>.New
            .With(d => d.A, new LocusInfo<string> {Position1 = A1GGroup, Position2 = A2GGroup})
            .With(d => d.B, new LocusInfo<string> {Position1 = B1GGroup, Position2 = B2GGroup})
            .With(d => d.C, new LocusInfo<string> {Position1 = C1GGroup, Position2 = C2GGroup})
            .With(d => d.Dpb1, new LocusInfo<string> {Position1 = null, Position2 = null})
            .With(d => d.Dqb1, new LocusInfo<string> {Position1 = Dqb11GGroup, Position2 = Dqb12GGroup})
            .With(d => d.Drb1, new LocusInfo<string> {Position1 = Drb11GGroup, Position2 = Drb12GGroup});
    }
}