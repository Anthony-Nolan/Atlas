using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using MoreLinq;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    internal static class BuilderDefaults
    {
        public const string HlaName = "hla";
        public const decimal Likelihood = 0.000001m;
        public const HaplotypeTypingCategory TypingCategory = HaplotypeTypingCategory.SmallGGroup;
    }

    internal class ImputedGenotypesBuilder
    {
        private ImputedGenotypes imputedGenotypes;

        public ImputedGenotypesBuilder()
        {
            imputedGenotypes = new ImputedGenotypes
            {
                Genotypes = new List<ImputedGenotype>(),
                SumOfLikelihoods = 0m
            };
        }

        // One genotype carrying its own name form and likelihood - the shape a consumer reads, with nothing to rejoin.
        public ImputedGenotypesBuilder Default()
        {
            imputedGenotypes = new ImputedGenotypes
            {
                Genotypes = new List<ImputedGenotype>
                {
                    new(
                        new KnownTypingCategoryGenotypeBuilder(BuilderDefaults.HlaName).Build(),
                        new PhenotypeInfoBuilder<string>(BuilderDefaults.HlaName).Build(),
                        BuilderDefaults.Likelihood)
                },
                SumOfLikelihoods = BuilderDefaults.Likelihood
            };

            return this;
        }

        public ImputedGenotypes Build()
        {
            return imputedGenotypes;
        }
    }

    internal class KnownTypingCategoryGenotypeBuilder : PhenotypeInfoBuilder<HlaAtKnownTypingCategory>
    {
        public KnownTypingCategoryGenotypeBuilder(string hlaName)
            : base(new HlaAtKnownTypingCategory(hlaName, BuilderDefaults.TypingCategory))
        {
        }
    }

    internal class GenotypeAtDesiredResolutionsBuilder
    {
        private GenotypeAtDesiredResolutions genotypeAtDesiredResolutions;

        public GenotypeAtDesiredResolutionsBuilder Default()
        {
            // Built from an ImputedGenotype, the same way GenotypeConverter builds one, so HaplotypeResolution and
            // GenotypeLikelihood come from one source rather than two independent literals.
            var genotype = new ImputedGenotype(
                new KnownTypingCategoryGenotypeBuilder(BuilderDefaults.HlaName).Build(),
                new PhenotypeInfoBuilder<string>(BuilderDefaults.HlaName).Build(),
                BuilderDefaults.Likelihood);

            genotypeAtDesiredResolutions = new GenotypeAtDesiredResolutions(genotype, new PhenotypeInfo<string>(BuilderDefaults.HlaName));

            return this;
        }

        public GenotypeAtDesiredResolutions Build()
        {
            return genotypeAtDesiredResolutions;
        }
    }
}