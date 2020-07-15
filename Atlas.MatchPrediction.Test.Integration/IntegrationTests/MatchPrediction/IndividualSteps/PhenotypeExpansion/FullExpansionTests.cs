using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable InconsistentNaming - want to avoid calling "G groups" "gGroup", as "g" groups are a distinct thing 

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    /// <summary>
    /// This suite focuses on testing that we can cope with all expected hla resolutions.
    /// Tests do not provide a set of allowed haplotype values for simplicity, which is not recommended for actual usage of the expander.
    /// </summary>
    [TestFixture]
    public class FullExpansionTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;

        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private readonly ISet<Locus> DefaultAllowedLoci = Config.LocusSettings.MatchPredictionLoci.ToHashSet();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            compressedPhenotypeExpander = DependencyInjection.DependencyInjection.Provider.GetService<ICompressedPhenotypeExpander>();
        }

        [TestCase("02:09", "02:01:01G")]
        [TestCase("02:07:01", "02:07:01G")]
        [TestCase("11:01:01:01", "11:01:01G")]
        public async Task ExpandCompressedPhenotype_WhenNoAmbiguousAlleles_ReturnsExpectedGenotype(string allele, string GGroup)
        {
            // Alleles chosen in test cases for Locus A
            const Locus locus = Locus.A;

            var alleleGGroupPairs = new PhenotypeInfoBuilder<AlleleWithGGroup>(Alleles.UnambiguousAlleleDetails)
                .WithDataAt(locus, new AlleleWithGGroup {Allele = allele, GGroup = GGroup})
                .Build();

            var phenotype = alleleGGroupPairs.Alleles();
            var expectedGenotypes = alleleGGroupPairs.GGroups();

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            actualGenotypes.Single().Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenAlleleStringOfNamesPresent_ReturnsExpectedGenotypes()
        {
            // Alleles chosen for Locus A
            const Locus locus = Locus.A;

            const string alleleString = "02:09/02:04";
            const string GGroup1 = "02:01:01G";
            const string GGroup2 = "02:04:01G";

            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(locus, alleleString).Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, GGroup1).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, new LocusInfo<string> {Position1 = GGroup2, Position2 = GGroup1}).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, new LocusInfo<string> {Position1 = GGroup1, Position2 = GGroup2}).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, GGroup2).Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenAlleleStringOfSubtypesPresent_ReturnsExpectedGenotypes()
        {
            // Alleles chosen for Locus A
            const Locus locus = Locus.A;

            const string alleleString = "02:09/04";
            const string GGroup1 = "02:01:01G";
            const string GGroup2 = "02:04:01G";

            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(locus, alleleString).Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, GGroup1).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, new LocusInfo<string> {Position1 = GGroup2, Position2 = GGroup1}).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, new LocusInfo<string> {Position1 = GGroup1, Position2 = GGroup2}).Build(),
                DefaultUnambiguousGGroupsBuilder.WithDataAt(locus, GGroup2).Build()
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenMixOfAmbiguousAllelesPresent_ReturnsExpectedGenotypes()
        {
            const string ambiguousAlleleAtB = "15:19/146";
            var ambiguousGGroupsAtB = new List<string> {"15:12:01G", "15:01:01G"};

            const string ambiguousAlleleAtC = "01:03/01:44";
            var ambiguousGGroupsAtC = new List<string> {"01:03:01G", "01:02:01G"};

            var phenotype = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, ambiguousAlleleAtB)
                .WithDataAt(Locus.C, LocusPosition.Two, ambiguousAlleleAtC)
                .Build();

            var expectedGenotypes = new List<PhenotypeInfo<string>>
            {
                DefaultUnambiguousGGroupsBuilder
                    .WithDataAt(Locus.B, LocusPosition.One, ambiguousGGroupsAtB.First())
                    .WithDataAt(Locus.C, LocusPosition.Two, ambiguousGGroupsAtC.First())
                    .Build(),
                DefaultUnambiguousGGroupsBuilder
                    .WithDataAt(Locus.B, LocusPosition.One, ambiguousGGroupsAtB.First())
                    .WithDataAt(Locus.C, LocusPosition.Two, ambiguousGGroupsAtC.Last())
                    .Build(),
                DefaultUnambiguousGGroupsBuilder
                    .WithDataAt(Locus.B, LocusPosition.One, ambiguousGGroupsAtB.Last())
                    .WithDataAt(Locus.C, LocusPosition.Two, ambiguousGGroupsAtC.First())
                    .Build(),
                DefaultUnambiguousGGroupsBuilder
                    .WithDataAt(Locus.B, LocusPosition.One, ambiguousGGroupsAtB.Last())
                    .WithDataAt(Locus.C, LocusPosition.Two, ambiguousGGroupsAtC.Last())
                    .Build(),
            };

            var actualGenotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            actualGenotypes.Should().BeEquivalentTo(expectedGenotypes);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenMacPresent_ExpandsMac()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv
            const string mac = "01:AC";

            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, mac).Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            // The two 2-field alleles represented by the MAC cover 86 G-Groups
            genotypes.Should().HaveCount(86);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenXXCodePresent_ExpandsXXCode()
        {
            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "01:XX").Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            genotypes.Should().HaveCount(303);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenSerologyPresent_ExpandsSerology()
        {
            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, "82").Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            genotypes.Should().HaveCount(4);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenPGroupPresent_ExpandsPGroup()
        {
            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "23:03P").Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            genotypes.Should().HaveCount(2);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenGGroupPresent_DoesNotExpandGGroup()
        {
            var phenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "02:02:01G").Build();

            var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultAllowedLoci);

            genotypes.Should().HaveCount(1);
        }

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousGGroupsBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}