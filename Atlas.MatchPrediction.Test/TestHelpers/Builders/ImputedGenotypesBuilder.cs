﻿using System.Collections.Generic;
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
                GenotypeLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>(),
                Genotypes = new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>(),
                SumOfLikelihoods = 0m
            };
        }

        public ImputedGenotypesBuilder Default()
        {
            imputedGenotypes = new ImputedGenotypes
            {
                GenotypeLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>
                {
                    {new PhenotypeInfoBuilder<string>(BuilderDefaults.HlaName).Build(), BuilderDefaults.Likelihood}
                },
                Genotypes = new[] { new KnownTypingCategoryGenotypeBuilder(BuilderDefaults.HlaName).Build() }.ToHashSet(),
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
            var haplotypeResolutions = new KnownTypingCategoryGenotypeBuilder(BuilderDefaults.HlaName).Build();
            genotypeAtDesiredResolutions = new GenotypeAtDesiredResolutions(
                haplotypeResolutions, 
                new PhenotypeInfo<string>(BuilderDefaults.HlaName),
                BuilderDefaults.Likelihood);

            return this;
        }

        public GenotypeAtDesiredResolutions Build()
        {
            return genotypeAtDesiredResolutions;
        }
    }
}