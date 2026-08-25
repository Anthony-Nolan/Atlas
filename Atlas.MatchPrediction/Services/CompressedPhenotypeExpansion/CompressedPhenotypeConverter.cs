using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HlaConversion;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion
{
    internal interface ICompressedPhenotypeConverter
    {
        /// <summary>
        /// The subject's phenotype as the allele groups of one typing category.
        ///
        /// <para>
        /// ATL-233 T5: one category per call, where this used to convert to all three on every call. Which categories
        /// are worth converting is knowledge the caller has and this class does not - it is a property of the haplotype
        /// frequency set, not of the phenotype - so the caller asks for what it will read. See
        /// <c>CompressedPhenotypeExpander.ExpandCompressedPhenotype</c> for which those are and why.
        /// </para>
        /// </summary>
        /// <returns>
        /// Excluded loci will not be converted, and will be set to `null`.
        /// Provided `null`s will be preserved.
        /// </returns>
        Task<PhenotypeInfo<ISet<string>>> ConvertPhenotype(
            CompressedPhenotypeExpanderInput input,
            HaplotypeTypingCategory category);
    }

    internal class CompressedPhenotypeConverter : ICompressedPhenotypeConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IHlaToTargetCategoryConverter converter;

        public CompressedPhenotypeConverter(
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory, IHlaToTargetCategoryConverter converter)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.converter = converter;
        }

        /// <inheritdoc />
        public async Task<PhenotypeInfo<ISet<string>>> ConvertPhenotype(
            CompressedPhenotypeExpanderInput input,
            HaplotypeTypingCategory category)
        {
            // Both are cache reads (HlaMetadataDictionaryFactory.BuildDictionary is a GetOrAdd), so resolving them per
            // category rather than once per donor costs nothing measurable and keeps this method free of per-request state.
            var hfSetHmd = hlaMetadataDictionaryFactory.BuildDictionary(input.HfSetHlaNomenclatureVersion);

            var matchingHlaVersion = input.MatchPredictionParameters.MatchingAlgorithmHlaNomenclatureVersion;
            var matchingHmd = matchingHlaVersion == null ? null : hlaMetadataDictionaryFactory.BuildDictionary(matchingHlaVersion);

            return await ConvertPhenotypeToTargetCategory(input, hfSetHmd, matchingHmd, category);
        }

        private async Task<PhenotypeInfo<ISet<string>>> ConvertPhenotypeToTargetCategory(
            CompressedPhenotypeExpanderInput expanderInput,
            IHlaMetadataDictionary hfSetHmd,
            IHlaMetadataDictionary matchingHmd,
            HaplotypeTypingCategory category)
        {
            const string stage = "Conversion of compressed phenotype to target HLA category";

            var converterInput = new HlaConverterInput
            {
                HfSetHmd = hfSetHmd,
                MatchingAlgorithmHmd = matchingHmd,
                StageToLog = stage,
                TargetHlaCategory = category.ToHlaTypingCategory().ToTargetHlaCategory()
            };

            return await expanderInput.Phenotype.MapAsync(async (locus, _, hla) =>
            {
                if (!expanderInput.MatchPredictionParameters.AllowedLoci.Contains(locus) || hla == null )
                {
                    return null;
                }

                // ATL-233 T5: this used to run for 3 categories x allowed loci x 2 positions - 30 lookups on the
                // 5-locus key - for every donor. It now runs for what the caller actually asked for, which is 10
                // while every HF set holds a single typing category.
                return (ISet<string>)(await converter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla)).ToHashSet();
            });
        }
    }
}
