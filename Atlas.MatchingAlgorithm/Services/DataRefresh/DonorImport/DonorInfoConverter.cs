using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport
{
    public interface IDonorInfoConverter
    {
        Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName);
    }

    public class DonorInfoConverter :
        DonorBatchProcessor<SearchableDonorInformation, DonorInfo>,
        IDonorInfoConverter
    {
        private readonly IMatchingAlgorithmImportLogger logger;

        public DonorInfoConverter(IMatchingAlgorithmImportLogger logger) : base(logger)
        {
            this.logger = logger;
        }

        public async Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName)
        {
            // DonorInfoConversion is one of the largest single slices of the whole refresh, at a couple of hundred
            // microseconds per donor to copy 18 fields. This splits it into its two halves so the next question -
            // "is that FluentValidation or the mapping?" - has an answer.
            //
            // Deliberately accumulated as raw Stopwatch ticks and emitted ONCE PER BATCH, rather than wrapping each
            // half in a metric timer. A metric call per donor per half would be ~88M SendMetric calls (each with a
            // dimension-dictionary allocation) per refresh: seconds of pure overhead, and enough Gen0 garbage to
            // corrupt the very allocation-pressure numbers the runtime sampler exists to measure. Stopwatch
            // timestamps are ~20ns and allocate nothing.
            //
            // The batch is processed strictly sequentially by the base class, so the closed-over counters need no
            // synchronisation.
            var validationTicks = 0L;
            var mappingTicks = 0L;

            var result = await ProcessBatchAsyncWithAnticipatedExceptions<ValidationException>(
                donorInfos,
                async info =>
                {
                    var validationStart = Stopwatch.GetTimestamp();
                    try
                    {
                        await new SearchableDonorInformationValidator().ValidateAndThrowAsync(info);
                    }
                    finally
                    {
                        // In a finally so a donor that FAILS validation still contributes the time it cost. Without
                        // this the two halves would not reconcile against DonorInfoConversion on a run with failures.
                        validationTicks += Stopwatch.GetTimestamp() - validationStart;
                    }

                    var mappingStart = Stopwatch.GetTimestamp();
                    var converted = info.ToDonorInfo();
                    mappingTicks += Stopwatch.GetTimestamp() - mappingStart;

                    return converted;
                },
                info => new FailedDonorInfo(info)
                {
                    AtlasDonorId = info.DonorId
                },
                failureEventName);

            logger.SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Stopwatch.GetElapsedTime(0, validationTicks).TotalMilliseconds,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorValidation));

            logger.SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Stopwatch.GetElapsedTime(0, mappingTicks).TotalMilliseconds,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_DonorMapping));

            return result;
        }
    }
}
