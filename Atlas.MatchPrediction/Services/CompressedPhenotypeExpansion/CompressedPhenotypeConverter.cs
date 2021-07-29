using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion
{
    internal interface ICompressedPhenotypeConverter
    {
        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertHla"/> for each HLA in a PhenotypeInfo, at selected loci.
        /// </summary>
        /// <returns>
        /// Excluded loci will not be converted, and will be set to `null`.
        /// Provided `null`s will be preserved.
        /// An empty list will be returned where HLA cannot be converted, e.g., an allele could not be found in the HMD due to being renamed in a later HLA version.
        /// </returns>
        Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertPhenotype(
            IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> compressedPhenotype,
            TargetHlaCategory targetHlaCategory,
            ISet<Locus> allowedLoci
        );
    }

    internal class CompressedPhenotypeConverter : ICompressedPhenotypeConverter
    {
        private readonly ILogger logger;

        public CompressedPhenotypeConverter(
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger)
        {
            this.logger = logger;
        }
        
        /// <inheritdoc />
        public async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertPhenotype(
            IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> compressedPhenotype,
            TargetHlaCategory targetHlaCategory,
            ISet<Locus> allowedLoci)
        {
            return await compressedPhenotype.MapAsync(async (locus, _, hla) =>
            {
                if (!allowedLoci.Contains(locus) || hla == null)
                {
                    return null;
                }

                try
                {
                    return await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory);
                }
                // All HMD exceptions are being caught and suppressed here, under the assumption that the subject's HLA has already been
                // validated by the matching algorithm component, and the only reason the typing is missing
                // from the HMD is due to the matching algorithm and HF set being on different nomenclature versions.
                // See https://github.com/Anthony-Nolan/Atlas/issues/636 for more info.
                // Note: if the MPA endpoint is ever added to the Public API to allow it to be run independently of matching,
                // then the above assumption no longer stands; the possibility of invalid HLA being submitted to the MPA directly must be handled.
                catch (HlaMetadataDictionaryException exception)
                {
                    logger.SendEvent(new HlaConversionFailureEventModel(
                        locus,
                        hla,
                        hlaMetadataDictionary.ActiveHlaNomenclatureVersion,
                        targetHlaCategory, 
                        "Conversion of compressed phenotype to target HLA category",
                        exception));

                    //TODO issue #637 - re-attempt HLA conversion using other approaches

                    return new List<string>();
                }
            });
        }
    }
}