using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using MoreLinq.Extensions;

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
        Task<PhenotypeInfo<ISet<string>>> ConvertPhenotype(
            IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> compressedPhenotype,
            TargetHlaCategory targetHlaCategory,
            ISet<Locus> allowedLoci
            );
    }

    internal class CompressedPhenotypeConverter : ICompressedPhenotypeConverter
    {
        private readonly ILogger logger;
        private readonly MatchPredictionAlgorithmSettings settings;

        public CompressedPhenotypeConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            MatchPredictionAlgorithmSettings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }
        
        /// <inheritdoc />
        public async Task<PhenotypeInfo<ISet<string>>> ConvertPhenotype(IHlaMetadataDictionary hlaMetadataDictionary,
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
                    return (ISet<string>) (await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory)).ToHashSet();
                }
                catch (HlaMetadataDictionaryException exception) when (settings.SuppressCompressedPhenotypeConversionExceptions)
                {
                    logger.SendEvent(new HlaConversionFailureEventModel(
                        locus,
                        hla,
                        hlaMetadataDictionary.HlaNomenclatureVersion,
                        targetHlaCategory, 
                        "Conversion of compressed phenotype to target HLA category",
                        exception));

                    //TODO issue #637 - re-attempt HLA conversion using other approaches

                    return new HashSet<string>();
                }
            });
        }
    }
}