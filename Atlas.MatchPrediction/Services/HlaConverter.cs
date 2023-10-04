using System;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services
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

        public ConversionPaths? ConversionPath { get; set; }

        public TargetHlaCategory? TargetHlaCategory { get; set; }

        /// <summary>
        /// Match prediction stage - used when logging conversion failures
        /// </summary>
        public string StageToLog { get; set; }

        public enum ConversionPaths
        {
            AnyHlaCategoryToTargetCategory,
            GGroupToPGroup,
            SmallGGroupToPGroup
        }
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

    internal class HlaConverter : IHlaConverter
    {
        private readonly ILogger logger;

        public HlaConverter(
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
                var converterFunc = ConverterFunctionSelector(input, hmd);

                try
                {
                    var convertedHla = await converterFunc(locus, hla);
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

        private static Func<Locus, string, Task<IEnumerable<string>>> ConverterFunctionSelector(HlaConverterInput input, IHlaMetadataDictionary hmd)
        {
            return input.ConversionPath switch
            {
                HlaConverterInput.ConversionPaths.AnyHlaCategoryToTargetCategory => async (locus, hlaName) =>
                {
                    if (input.TargetHlaCategory == null)
                    {
                        throw new ArgumentNullException(nameof(input.TargetHlaCategory));
                    }
                    return await hmd.ConvertHla(locus, hlaName, input.TargetHlaCategory.Value);
                },

                HlaConverterInput.ConversionPaths.GGroupToPGroup => async (locus, hlaName) =>
                    new[] { await hmd.ConvertGGroupToPGroup(locus, hlaName) },

                HlaConverterInput.ConversionPaths.SmallGGroupToPGroup => async (locus, hlaName) =>
                    new[] { await hmd.ConvertSmallGGroupToPGroup(locus, hlaName) },

                _ => throw new ArgumentOutOfRangeException(nameof(input.ConversionPath), input.ConversionPath, null)
            };
        }
    }
}