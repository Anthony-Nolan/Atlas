using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Atlas.MatchPrediction.Test.Integration.Resources.Alleles.Alleles;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.PhenotypeExpansion
{
    internal abstract class FilteredExpansionTestsBase
    {
        protected const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;
        protected static readonly ISet<Locus> DefaultLoci = LocusSettings.MatchPredictionLoci;

        // The GGroups represented by the default alleles in UnambiguousAlleleDetails could theoretically be split into 16 haplotypes. 
        // We are only using two (as if allele phase was represented in the raw data) for simplicity
        protected static LociInfoBuilder<string> HaplotypeBuilder1 => new LociInfoBuilder<string>(UnambiguousAlleleDetails.GGroups().Split().Item1);
        protected static LociInfoBuilder<string> HaplotypeBuilder2 => new LociInfoBuilder<string>(UnambiguousAlleleDetails.GGroups().Split().Item2);

        protected ICompressedPhenotypeExpander Expander;

        private readonly bool suppressCompressedPhenotypeConversionExceptions;

        /// <param name="suppressCompressedPhenotypeConversionExceptions">
        /// Determines whether tests should be run with HLA exceptions being suppressed during compressed phenotype conversion or allowed to throw.
        /// </param>
        protected FilteredExpansionTestsBase(bool suppressCompressedPhenotypeConversionExceptions)
        {
            this.suppressCompressedPhenotypeConversionExceptions = suppressCompressedPhenotypeConversionExceptions;
        }

        [SetUp]
        public void SetUp()
        {
            var logger = DependencyInjection.DependencyInjection.Provider.GetService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            var factory = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMetadataDictionaryFactory>();

            var settings = new MatchPredictionAlgorithmSettings
                { SuppressCompressedPhenotypeConversionExceptions = suppressCompressedPhenotypeConversionExceptions };
            var converter = new CompressedPhenotypeConverter(logger, settings);

            Expander = new CompressedPhenotypeExpander(logger, factory, converter);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_DoesNotIncludesSwappedGenotypes()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles()).Build();
            var haplotypes = new List<LociInfo<string>> { HaplotypeBuilder1.Build(), HaplotypeBuilder2.Build() };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                {
                    GGroup = haplotypes,
                    PGroup = haplotypes,
                    SmallGGroup = haplotypes
                }
            });

            // Expect (a,b) to only be returned once - homozygous correction factor takes care of this.
            genotypes.Count.Should().Be(1);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_ForSingleHaplotype_DoesNotIncludeHomozygousPairTwice()
        {
            var haplotypeAsLociInfo = UnambiguousAlleleDetails.Alleles().Split().Item1;
            var phenotype = new PhenotypeInfo<string>(haplotypeAsLociInfo, haplotypeAsLociInfo);
            var haplotypes = new List<LociInfo<string>> { HaplotypeBuilder1.Build() };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

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

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            // This G Group is represented by the patient HLA (01:XX), but is not present in the HF set 
            const string expectedAbsentGGroup = "01:01:02";
            genotypes.Should().NotContain(x => x.A.Position1.Hla == expectedAbsentGGroup || x.A.Position2.Hla == expectedAbsentGGroup);
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

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            genotypes.Count.Should().Be(1);
        }

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

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            // Of two matching haplotypes, four possible combinations as diplotypes: x & y => (xx)/(xy)/(yy)
            genotypes.Count.Should().Be(3);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenRepresentedByBothGAndPGroupHaplotypes_ExpandsCorrectly()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var gGroupHaplotypes = new List<LociInfo<string>>
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
            var pGroupHaplotypes = new List<LociInfo<string>>
            {
                // Two haplotypes matching phenotype
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:01P")
                    .WithDataAt(Locus.B, "08:01P")
                    .WithDataAt(Locus.C, "07:01P")
                    .WithDataAt(Locus.Dqb1, "02:01P")
                    .WithDataAt(Locus.Drb1, "03:01P")
                    .Build(),
                new LociInfoBuilder<string>()
                    .WithDataAt(Locus.A, "01:09P")
                    .WithDataAt(Locus.B, "08:01P")
                    .WithDataAt(Locus.C, "07:01P")
                    .WithDataAt(Locus.Dqb1, "02:01P")
                    .WithDataAt(Locus.Drb1, "03:01P")
                    .Build(),

                // Two haplotypes that do not match genotype
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build()
            };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                {
                    GGroup = gGroupHaplotypes,
                    PGroup = pGroupHaplotypes,
                    SmallGGroup = new List<LociInfo<string>>()
                }
            });

            // Of four matching haplotypes (at 2 resolutions) - nCr (including self-pairs) = 10 possibilities
            genotypes.Count.Should().Be(10);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WhenRepresentedBySmallGResolutionHaplotypes_ExpandsCorrectly()
        {
            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

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

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                {
                    PGroup = new List<LociInfo<string>>(),
                    GGroup = new List<LociInfo<string>>(),
                    SmallGGroup = smallGGroupHaplotypes
                }
            });

            genotypes.Count.Should().Be(3);
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WithExcludedLocus_DoesNotExpandAtThatLocus()
        {
            const Locus excludedLocus = Locus.C;
            const string otherGGroupAtMissingLocus = "01:01:01G";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(excludedLocus, (string)null)
                .Build();

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
                HaplotypeBuilder2.WithDataAt(excludedLocus, otherGGroupAtMissingLocus).Build(),
            };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci.Except(new List<Locus> { excludedLocus }).ToHashSet(),
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            genotypes.Count.Should().Be(1);
            genotypes.Single().GetLocus(excludedLocus).Position1And2Null().Should().BeTrue();
        }

        [Test]
        public async Task ExpandCompressedPhenotype_WithEmptyLocus_ExpandsToAllMatchingDiplotypesAtThatLocus()
        {
            const Locus missingLocus = Locus.C;
            const string otherGGroupAtMissingLocus = "01:01:01G";

            var phenotype = new PhenotypeInfoBuilder<string>(UnambiguousAlleleDetails.Alleles())
                .WithDataAt(missingLocus, (string)null)
                .Build();

            var phenotypeWithoutMissingLoci = new PhenotypeInfo<string>(UnambiguousAlleleDetails.Alleles());

            var haplotypes = new List<LociInfo<string>>
            {
                HaplotypeBuilder1.Build(),
                HaplotypeBuilder2.Build(),
                HaplotypeBuilder1.WithDataAt(missingLocus, otherGGroupAtMissingLocus).Build(),
                HaplotypeBuilder2.WithDataAt(missingLocus, otherGGroupAtMissingLocus).Build(),
            };

            var genotypes = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotype,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            var genotypesWithoutMissingLoci = await Expander.ExpandCompressedPhenotype(new ExpandCompressedPhenotypeInput
            {
                Phenotype = phenotypeWithoutMissingLoci,
                AllowedLoci = DefaultLoci,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                { GGroup = haplotypes, PGroup = haplotypes, SmallGGroup = haplotypes }
            });

            genotypes.Count.Should().Be(4);

            // Strictly this assertion is not necessary, but it could be useful for people reading this test to understand what is being tested here,
            // rather than blindly trusting the snapshot of 4 matching genotypes.
            genotypesWithoutMissingLoci.Count().Should().BeLessThan(genotypes.Count);
        }
    }
}