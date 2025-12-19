using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HlaConversion;
using MoreLinq.Extensions;
using ConvertedPhenotype = Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion.DataByResolution<Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<System.Collections.Generic.ISet<string>>>;

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion
{
    internal interface ICompressedPhenotypeConverter
    {
        /// <returns>
        /// Excluded loci will not be converted, and will be set to `null`.
        /// Provided `null`s will be preserved.
        /// </returns>
        Task<ConvertedPhenotype> ConvertPhenotype(CompressedPhenotypeExpanderInput input);
    }

    internal class CompressedPhenotypeConverter : ICompressedPhenotypeConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IHlaToTargetCategoryConverter converter;
        private const string newAllele = "NEW";

        public CompressedPhenotypeConverter(
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory, IHlaToTargetCategoryConverter converter)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.converter = converter;
        }

        /// <inheritdoc />
        public async Task<ConvertedPhenotype> ConvertPhenotype(CompressedPhenotypeExpanderInput input)
        {
            var hfSetHmd = hlaMetadataDictionaryFactory.BuildDictionary(input.HfSetHlaNomenclatureVersion);

            var matchingHlaVersion = input.MatchPredictionParameters.MatchingAlgorithmHlaNomenclatureVersion;
            var matchingHmd = matchingHlaVersion == null ? null : hlaMetadataDictionaryFactory.BuildDictionary(matchingHlaVersion);

            return await new DataByResolution<bool>().MapAsync(async (category, _) =>
                await ConvertPhenotypeToTargetCategory(input, hfSetHmd, matchingHmd, category));
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

                return (ISet<string>)(await converter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla)).ToHashSet();
            });
        }
    }
}