using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.HlaConversion
{
    internal class HlaConverterInput
    {
        /// <summary>
        /// HMD for HLA version that HF set is encoded in.
        /// </summary>
        public IHlaMetadataDictionary HfSetHmd { get; set; }

        /// <summary>
        /// HMD for HLA version used by matching algorithm.
        /// </summary>
        public IHlaMetadataDictionary MatchingAlgorithmHmd { get; set; }

        public bool DoNotRetryLookupUsingMatchingAlgorithmHmd =>
            MatchingAlgorithmHmd == null || HfSetHmd.HlaNomenclatureVersion == MatchingAlgorithmHmd.HlaNomenclatureVersion;

        public TargetHlaCategory? TargetHlaCategory { get; set; }

        /// <summary>
        /// Match prediction stage - used when logging conversion failures
        /// </summary>
        public string StageToLog { get; set; }
    }

    internal interface IHlaConverter
    {
        ///<summary>
        /// Will first attempt to convert HLA using <paramref name="input.HfSetHmd"/> and then will retry conversion on failure using <paramref name="input.MatchingAlgorithmHmd"/>.
        /// </summary>
        /// <returns>
        /// 1) First tries to convert HLA using <paramref name="input.HfSetHmd"/> and returns result if success, else, logs the failure.
        /// 2) On failure, if <paramref name="input.DoNotRetryLookupUsingMatchingAlgorithmHmd"/> is `true`, then returns an empty set without throwing an exception.
        /// 3) Otherwise, attempts a second conversion using <paramref name="input.MatchingAlgorithmHmd"/>, and returns HLA if success.
        /// 4) If conversion second attempt also fails, logs the failure and returns an empty set without throwing an exception.
        /// </returns>
        Task<IEnumerable<string>> ConvertHlaWithLoggingAndRetryOnFailure(HlaConverterInput input, Locus locus, string hla);
    }

    internal abstract class HlaConverterBase : IHlaConverter
    {
        private readonly ILogger logger;

        protected HlaConverterBase(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.logger = logger;
        }

        public async Task<IEnumerable<string>> ConvertHlaWithLoggingAndRetryOnFailure(HlaConverterInput input, Locus locus, string hla)
        {
            var hfSetHmdResult = await TryConvertHla(input.HfSetHmd);

            return hfSetHmdResult.WasSuccessful || input.DoNotRetryLookupUsingMatchingAlgorithmHmd
                ? hfSetHmdResult.ConvertedHla
                : (await TryConvertHla(input.MatchingAlgorithmHmd)).ConvertedHla;

            async Task<(bool WasSuccessful, IEnumerable<string> ConvertedHla)> TryConvertHla(IHlaMetadataDictionary hmd)
            {
                try
                {
                    var convertedHla = await ConvertHla(input.TargetHlaCategory, locus, hla, hmd);
                    return (true, convertedHla);
                }
                catch (HlaMetadataDictionaryException exception)
                {
                    logger.SendEvent(new HlaConversionFailureEventModel(
                        locus,
                        hla,
                        hmd.HlaNomenclatureVersion,
                        input.TargetHlaCategory,
                        input.StageToLog,
                        exception));

                    return (false, new List<string>());
                }
            }
        }

        protected abstract Task<IEnumerable<string>> ConvertHla(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd);
    }
}