using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchPrediction.ExternalInterface;

public interface IMatchPredictionAlgorithm
{
    public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

    /// <returns>A dictionary of donorId:filenames in blob storage where the per-donor results can be located.</returns>
    public Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatch(MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput);

    public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput);
}

internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
{
    private readonly IMatchProbabilityService matchProbabilityService;
    private readonly IGenotypeSetService genotypeSetService;
    private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
    private readonly ISearchDonorResultUploader resultUploader;
    private readonly IAtlasLogger logger;

    public MatchPredictionAlgorithm(
        IMatchProbabilityService matchProbabilityService,
        IGenotypeSetService genotypeSetService,
        IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
        IHaplotypeFrequencyService haplotypeFrequencyService,
        ISearchDonorResultUploader resultUploader)
    {
        this.matchProbabilityService = matchProbabilityService;
        this.genotypeSetService = genotypeSetService;
        this.logger = logger;
        this.haplotypeFrequencyService = haplotypeFrequencyService;
        this.resultUploader = resultUploader;
    }

    /// <inheritdoc />
    public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
    {
        using (logger.RunTimed("Run Match Prediction Algorithm"))
        {
            var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(singleDonorMatchProbabilityInput);
            var result = await matchProbabilityService.CalculateMatchProbability(singleDonorMatchProbabilityInput, patientGenotypeSet);
            return result.Round(4);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatch(
        MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput)
    {
        using (logger.RunLongOperationWithTimer("Run Match Prediction Algorithm Batch", new LongLoggingSettings()))
        {
            var searchRequestId = multipleDonorMatchProbabilityInput.SearchRequestId;
            var fileNames = new Dictionary<int, string>();
            var matchProbabilityInputs = multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs.ToList();
            if (matchProbabilityInputs.Count == 0)
            {
                return fileNames;
            }

            var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(matchProbabilityInputs.First());

            foreach (var matchProbabilityInput in matchProbabilityInputs)
            {
                using (logger.RunTimed("Run Match Prediction Algorithm per donor"))
                {
                    var result = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput, patientGenotypeSet);
                    var matchProbabilityInputFileNames = await resultUploader.UploadSearchDonorResults(searchRequestId, matchProbabilityInput.Donor.DonorIds, result);
                    fileNames = fileNames.Merge(matchProbabilityInputFileNames);
                }
            }

            return fileNames;
        }
    }

    public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput)
    {
        return await haplotypeFrequencyService.GetHaplotypeFrequencySets(
            haplotypeFrequencySetInput.DonorInfo,
            haplotypeFrequencySetInput.PatientInfo);
    }

}