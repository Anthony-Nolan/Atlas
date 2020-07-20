using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    [TestFixture]
    internal class FilteredExpansion
    {
        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;
        private static readonly ISet<Locus> DefaultLoci = LocusSettings.MatchPredictionLoci.ToHashSet();

        // The GGroups represented by the default alleles in UnambiguousAlleleDetails could theoretically be split into 16 haplotypes. 
        // We are only using two (as if allele phase was represented in the raw data) for simplicity
        private static LociInfoBuilder<string> HaplotypeBuilder1 => new LociInfoBuilder<string>(UnambiguousAlleleDetails.GGroups().Split().Item1);
        private static LociInfoBuilder<string> HaplotypeBuilder2 => new LociInfoBuilder<string>(UnambiguousAlleleDetails.GGroups().Split().Item2);

        private ICompressedPhenotypeExpander expander;

        [SetUp]
        public void SetUp()
        {
            expander = DependencyInjection.DependencyInjection.Provider.GetService<ICompressedPhenotypeExpander>();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_DoesNotIncludesSwappedGenotypes()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles()).Build();
            var haplotypes = new List<LociInfo<string>> {HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build()};

            var genotypes = await expander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultLoci, haplotypes);

            // Expect (a,b) to only be returned once - homozygous correction factor takes care of this.
            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ForSingleHaplotype_DoesNotIncludeHomozygousPairTwice()
        {
            var haplotypeAsLociInfo = UnambiguousAlleleDetails.Alleles().Split().Item1;
            var phenotype = new PhenotypeInfo<string>(haplotypeAsLociInfo, haplotypeAsLociInfo);
            var haplotypes = new List<LociInfo<string>> {HaplotypeBuilder1.Build()};

            var genotypes = await expander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultLoci, haplotypes);

            // Expect (a,a) not to be duplicated
            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_DoesNotIncludeGenotypes_ThatAreNotRepresentedInHaplotypeSet()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, "01:XX")
                .Build();

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.WithDataAt(Locus.A, "01:01:01G").Build(),
                HaplotypeBuilder2.Build()
            };

            var genotypes = await expander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultLoci, haplotypes);

            // This G Group is represented by the patient HLA (01:XX), but is not present in the HF set 
            const string expectedAbsentGGroup = "01:01:02";
            genotypes.Should().NotContain(x => x.A.Position1 == expectedAbsentGGroup || x.A.Position2 == expectedAbsentGGroup);
        }

        [TestCaseSource(nameof(DefaultLoci))]
        public async Task ExpandCompressedPhenotype_WhenOneLocusExcluded_ReturnsCombinationsOfAllHaplotypesThatDifferOnlyAtExcludedLoci(Locus locus)
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles()).Build();

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(locus, "g-group-at-excluded-locus").Build()
            };

            var genotypes = await expander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultLoci, haplotypes);

            genotypes.Count.Should().Be(1);
        }

        // This test is fairly slow, as tests go: ~3 seconds.
        // With the naive approach, this would take *significantly* longer - the number of permutations would overflow a long!
        [Test]
        public async Task ExpandCompressedPhenotype_ForVeryAmbiguousGenotype_ExpandsCorrectly()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var haplotypes = new List<LociInfo<string>>
            {
                // Two haplotypes matching phenotype
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:01:01G")
                    .WithDataAt(Locus.B, "08:01:01G")
                    .WithDataAt(Locus.C, "07:01:01G")
                    .WithDataAt(Locus.Dqb1, "02:01:01G")
                    .WithDataAt(Locus.Drb1, "03:01:01G")
                    .Build(),
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:09:01G")
                    .WithDataAt(Locus.B, "08:01:01G")
                    .WithDataAt(Locus.C, "07:01:01G")
                    .WithDataAt(Locus.Dqb1, "02:01:01G")
                    .WithDataAt(Locus.Drb1, "03:01:01G")
                    .Build(),

                // Two haplotypes that do not match genotype
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build()
            };

            var genotypes = await expander.ExpandCompressedPhenotype(phenotype, HlaNomenclatureVersion, DefaultLoci, haplotypes);

            // Of two matching haplotypes, four possible combinations as diplotypes: x & y => (xx)/(xy)/(yy)
            genotypes.Count.Should().Be(3);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WithExcludedLocus_DoesNotExpandAtThatLocus()
        {
            const Locus excludedLocus = Locus.C;
            const string otherGGroupAtMissingLocus = "01:01:01G";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(excludedLocus, (string) null)
                .Build();

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
                HaplotypeBuilder2.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
            };

            var genotypes = await expander.ExpandCompressedPhenotype(
                phenotype,
                HlaNomenclatureVersion,
                DefaultLoci.Except(new List<Locus> {excludedLocus}).ToHashSet(),
                haplotypes
            );

            genotypes.Count.Should().Be(1);
            genotypes.Single().GetLocus(excludedLocus).Position1And2Null().Should().BeTrue();
        }
    }
}