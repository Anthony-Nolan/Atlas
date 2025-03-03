using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const char asterisk = '*';
        private const string allelePattern = "^\\*?\\d+(:\\d+){1,3}[ACLNQS]?$";

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

            if (matchingHmd != null)
            {
                input.Phenotype = await input.Phenotype.MapAsync<string>(async (locus, _, hla) =>
                {    
                    if (hla == null)
                    {
                        return hla;
                    }

                    if (!Regex.IsMatch(hla, allelePattern))
                    {
                        return hla;
                    }
                    
                    var renamedHla = await matchingHmd.GetCurrentAlleleNames(locus, hla);

                    if (renamedHla.Count() == 1 && !renamedHla.Any(x => x.Contains(hla.TrimStart(asterisk))))
                    {
                        return renamedHla.FirstOrDefault();
                    }

                    return hla;
                });
            }

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
                if (!expanderInput.MatchPredictionParameters.AllowedLoci.Contains(locus) || hla == null)
                {
                    return null;
                }

                return (ISet<string>)ToHashSetExtension.ToHashSet((await converter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla)));
            });
        }
    }
}