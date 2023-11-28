using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    internal abstract class CompressedPhenotypeExpanderTestsBase
    {
        protected const string HfSetHlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;
        protected static readonly ISet<Locus> DefaultLoci = LocusSettings.MatchPredictionLoci;

        // The GGroups represented by the default alleles in UnambiguousAlleleDetails could theoretically be split into 16 haplotypes. 
        // We are only using two (as if allele phase was represented in the raw data) for simplicity
        protected static LociInfoBuilder<string> HaplotypeBuilder1 => new(UnambiguousAlleleDetails.GGroups().Split().Item1);
        protected static LociInfoBuilder<string> HaplotypeBuilder2 => new(UnambiguousAlleleDetails.GGroups().Split().Item2);

        protected ICompressedPhenotypeExpander Expander;

        private readonly string matchingAlgorithmHlaNomenclatureVersion;

        /// <param name="matchingAlgorithmHlaNomenclatureVersion">
        /// Value for <see cref="MatchPredictionParameters.MatchingAlgorithmHlaNomenclatureVersion"/> when expanding phenotypes.
        /// Default is `null`.
        /// </param>
        protected CompressedPhenotypeExpanderTestsBase(string matchingAlgorithmHlaNomenclatureVersion = null)
        {
            this.matchingAlgorithmHlaNomenclatureVersion = matchingAlgorithmHlaNomenclatureVersion;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Expander = DependencyInjection.DependencyInjection.Provider.GetService<ICompressedPhenotypeExpander>();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_DoesNotIncludeSwappedGenotypes()
        {
            var setId = await ImportHaplotypeFrequencies(new[] { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() });

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles()).Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            // Expect (a,b) to only be returned once - homozygous correction factor takes care of this.
            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ForSingleHaplotype_DoesNotIncludeHomozygousPairTwice()
        {
            var setId = await ImportHaplotypeFrequencies(new[] { HaplotypeBuilder1.Build() });

            var haplotypeAsLociInfo = UnambiguousAlleleDetails.Alleles().Split().Item1;
            var phenotype = new PhenotypeInfo<string>(haplotypeAsLociInfo, haplotypeAsLociInfo);

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            // Expect (a,a) not to be duplicated
            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_DoesNotIncludeGenotypes_ThatAreNotRepresentedInHaplotypeSet()
        {
            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.WithDataAt(Locus.A, "01:01:01G").Build(),
                HaplotypeBuilder2.Build()
            };
            var setId = await ImportHaplotypeFrequencies(haplotypes);

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, LocusPosition.One, "01:XX")
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            // This G Group is represented by the patient HLA (01:XX), but is not present in the HF set 
            const string expectedAbsentGGroup = "01:01:02";
            genotypes.Should().NotContain(x => x.A.Position1.Hla == expectedAbsentGGroup || x.A.Position2.Hla == expectedAbsentGGroup);
        }

        [TestCaseSource(nameof(DefaultLoci))]
        public async Task ExpandCompressedPhenotype_WhenOneLocusExcluded_ReturnsCombinationsOfAllHaplotypesThatDifferOnlyAtExcludedLoci(Locus locus)
        {
            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(locus, "g-group-at-excluded-locus").Build()
            };
            var setId = await ImportHaplotypeFrequencies(haplotypes);

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles()).Build();
            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ForVeryAmbiguousGenotype_ExpandsCorrectly()
        {
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
            var setId = await ImportHaplotypeFrequencies(haplotypes);

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            // Of two matching haplotypes, four possible combinations as diplotypes: x & y => (xx)/(xy)/(yy)
            genotypes.Count.Should().Be(3);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenRepresentedBySmallGResolutionHaplotypes_ExpandsCorrectly()
        {
            var smallGGroupHaplotypes = new List<LociInfo<string>>
            {
                // Two haplotypes matching phenotype
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:01g")
                    .WithDataAt(Locus.B, "08:01g")
                    .WithDataAt(Locus.C, "07:01g")
                    .WithDataAt(Locus.Dqb1, "02:01g")
                    .WithDataAt(Locus.Drb1, "03:01g")
                    .Build(),
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:09")
                    .WithDataAt(Locus.B, "08:01g")
                    .WithDataAt(Locus.C, "07:01g")
                    .WithDataAt(Locus.Dqb1, "02:01g")
                    .WithDataAt(Locus.Drb1, "03:01g")
                    .Build(),

                // Two haplotypes that do not match genotype
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build()
            };
            var setId = await ImportHaplotypeFrequencies(smallGGroupHaplotypes, ImportTypingCategory.SmallGGroup);

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            genotypes.Count.Should().Be(3);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WithExcludedLocus_DoesNotExpandAtThatLocus()
        {
            const Locus excludedLocus = Locus.C;
            const string otherGGroupAtMissingLocus = "01:01:01G";

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
                HaplotypeBuilder2.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
            };
            var setId = await ImportHaplotypeFrequencies(haplotypes);

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(excludedLocus, (string)null)
                .Build();

            var allowedLoci = DefaultLoci.Except(new List<Locus> { excludedLocus }).ToHashSet();

            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(allowedLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act
            var genotypes = await Expander.ExpandCompressedPhenotype(input);

            genotypes.Count.Should().Be(1);
            genotypes.Single().GetLocus(excludedLocus).Position1And2Null().Should().BeTrue();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WithEmptyLocus_ExpandsToAllMatchingDiplotypesAtThatLocus()
        {
            const Locus emptyLocus = Locus.C;

            const string otherGGroupAtMissingLocus = "01:02:01G";
            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(emptyLocus, otherGGroupAtMissingLocus).Build(),
                HaplotypeBuilder2.WithDataAt(emptyLocus, otherGGroupAtMissingLocus).Build(),
            };
            var setId = await ImportHaplotypeFrequencies(haplotypes);

            // Arrange - empty locus
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(emptyLocus, (string)null)
                .Build();
            var input = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotype,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Arrange - no empty loci
            var phenotypeWithoutEmptyLoci = new PhenotypeInfo<string>(UnambiguousAlleleDetails.Alleles());
            var inputWithoutMissingLoci = new CompressedPhenotypeExpanderInput
            {
                Phenotype = phenotypeWithoutEmptyLoci,
                MatchPredictionParameters = new MatchPredictionParameters(DefaultLoci, matchingAlgorithmHlaNomenclatureVersion),
                HfSetHlaNomenclatureVersion = HfSetHlaNomenclatureVersion,
                HfSetId = setId
            };

            // Act - empty locus
            var genotypes = await Expander.ExpandCompressedPhenotype(input);
            // Act - no empty loci
            var genotypesWithoutMissingLoci = await Expander.ExpandCompressedPhenotype(inputWithoutMissingLoci);

            genotypes.Count.Should().Be(4);

            // Strictly this assertion is not necessary, but it could be useful for people reading this test to understand what is being tested here,
            // rather than blindly trusting the snapshot of 4 matching genotypes.
            genotypesWithoutMissingLoci.Count.Should().BeLessThan(genotypes.Count);
        }

        protected static async Task<int> ImportHaplotypeFrequencies(
            IEnumerable<LociInfo<string>> haplotypes,
            ImportTypingCategory typingCategory = ImportTypingCategory.LargeGGroup)
        {
            const decimal defaultFrequency = 0.0000001m;
            var frequencies = haplotypes.Select(h => new HaplotypeFrequency { Hla = h, Frequency = defaultFrequency });

            var randomizer = Randomizer.CreateRandomizer();
            return await HaplotypeFrequencyImporter.Import(
                frequencies, 
                HfSetHlaNomenclatureVersion,
                randomizer.GetString(10),
                randomizer.GetString(10),
                typingCategory,
                true);
        }
    }
}