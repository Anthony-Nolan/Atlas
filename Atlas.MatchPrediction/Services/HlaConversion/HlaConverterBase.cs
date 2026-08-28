using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
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
        private readonly IAtlasLogger logger;

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
                // No try/catch: the dictionary now answers "this name has no data" as a value, so
                // the common case costs no throw. What a `catch` would still be needed for is an infrastructure fault, and
                // those deliberately propagate now rather than being swallowed as a failed conversion: reading a
                // storage blip as "this HLA does not exist" would predict from an incomplete expansion, silently.
                var (wasFound, convertedHla) = await TryConvert(input.TargetHlaCategory, locus, hla, hmd);

                if (wasFound)
                {
                    return (true, convertedHla);
                }

                LogConversionFailure(hmd);

                return (false, new List<string>());
            }

            void LogConversionFailure(IHlaMetadataDictionary hmd)
            {
                // These five dimensions are the whole diagnostic: which name, at which locus, at which
                // nomenclature version, converting to what, during which stage. `exception.ToString()` used to be a
                // sixth - a full async stack, multiple kilobytes - and it added nothing they do not already say,
                // because the exception's own message was "Failed to lookup '<Hla>' at locus <Locus>".
                //
                // It was not free. This is not an error path in production: a 248-donor run reached it 11,538 times,
                // and those strings were possibly a larger cost than the throws they accompanied. There is no longer
                // an exception here to serialise in any case - see TryConvert below.
                logger.SendEvent("HLA Conversion Failed", LogLevel.Warn, new Dictionary<string, string>
                {
                    { "Locus", locus.ToString() },
                    { "Hla", hla },
                    { "HlaNomenclatureVersion", hmd.HlaNomenclatureVersion },
                    { nameof(TargetHlaCategory), input.TargetHlaCategory.ToString() },
                    { "Stage of Failure", input.StageToLog }
                });
            }
        }

        protected abstract Task<(bool WasFound, IEnumerable<string> ConvertedHla)> TryConvert(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd);
    }
}