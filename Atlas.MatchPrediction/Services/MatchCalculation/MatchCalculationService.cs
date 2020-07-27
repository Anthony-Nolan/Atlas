﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Utils;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        ///
        /// Patient genotype and donor genotype *MUST* be provided at G Group typing resolution.
        /// </returns>
        public Task<LociInfo<int?>> CalculateMatchCounts(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci);

        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        ///
        /// This is a performance-optimised version of <see cref="CalculateMatchCounts"/>, and as such has some more stringent requirements.
        /// 
        /// Patient genotype and donor genotype *MUST* be provided at a resolution for which match counts can be calculated by string comparison.
        /// i.e. PGroups, or G-Groups when a null allele is present.
        /// TODO: ATLAS-572: Properly tackle null alleles - the current approach will cover matching null alleles, but not the homozygous case. 
        /// </returns>
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci);
    }

    internal class MatchCalculationService : IMatchCalculationService
    {
        private readonly IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public MatchCalculationService(
            IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<LociInfo<int?>> CalculateMatchCounts(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var patientGenotypeAsSinglePGroups = await hlaMetadataDictionary.ConvertGGroupsToPGroups(patientGenotype, allowedLoci);
            var donorGenotypeAsSinglePGroups = await hlaMetadataDictionary.ConvertGGroupsToPGroups(donorGenotype, allowedLoci);

            return CalculateMatchCounts_Fast(patientGenotypeAsSinglePGroups, donorGenotypeAsSinglePGroups, allowedLoci);
        }

        // This method will be called millions of times in match prediction, and needs to stay as fast as possible. 
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci)
        {
            return new LociInfo<int?>().Map((locus, matchCount) =>
            {
                // TODO: ATLAS-595: Test switch performance in GetLocus?
                var patientHla = patientGenotype.GetLocus(locus);
                var donorHla = donorGenotype.GetLocus(locus);
                return allowedLoci.Contains(locus)
                    ? stringBasedLocusMatchCalculator.MatchCount(patientHla, donorHla, UntypedLocusBehaviour.Throw)
                    : (int?) null;
            });
        }
    }
}