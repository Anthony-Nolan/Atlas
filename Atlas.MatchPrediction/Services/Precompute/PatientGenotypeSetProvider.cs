using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;

namespace Atlas.MatchPrediction.Services.Precompute;

public interface IPatientGenotypeSetProvider
{
    /// <summary>
    /// Gets a batch's patient genotype set: the stored set when the batch can use it, otherwise computed live, as
    /// before ATL-426. Batches never store the patient set; the warm step does, once per search
    /// (<see cref="IPatientGenotypeSetWarmer"/>).
    /// </summary>
    /// <param name="input">Any of the batch's single donor inputs; only its patient data is used.</param>
    /// <param name="batchContext">The batch's context, from <see cref="IDonorGenotypeSetSourceResolver.Resolve"/>.</param>
    Task<(SubjectGenotypeSet GenotypeSet, PatientGenotypeSetSource Source)> Get(
        SingleDonorMatchProbabilityInput input,
        DonorGenotypeSetBatchContext batchContext);
}

internal class PatientGenotypeSetProvider : IPatientGenotypeSetProvider
{
    private readonly IGenotypeSetService genotypeSetService;
    private readonly IHaplotypeFrequencyLookupService haplotypeFrequencyService;

    public PatientGenotypeSetProvider(IGenotypeSetService genotypeSetService, IHaplotypeFrequencyLookupService haplotypeFrequencyService)
    {
        this.genotypeSetService = genotypeSetService;
        this.haplotypeFrequencyService = haplotypeFrequencyService;
    }

    /// <inheritdoc />
    public async Task<(SubjectGenotypeSet GenotypeSet, PatientGenotypeSetSource Source)> Get(
        SingleDonorMatchProbabilityInput input,
        DonorGenotypeSetBatchContext batchContext)
    {
        // The same lookup the live path makes, so the stale check compares with the set live computation would use.
        var patientFrequencySet = await haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(
            input.PatientFrequencySetMetadata ?? new FrequencySetMetadata());

        var source = batchContext.TryGetPrecomputedPatientGenotypeSet(patientFrequencySet.Id, out var storedGenotypeSet);
        if (source == PatientGenotypeSetSource.Precomputed)
        {
            return (storedGenotypeSet, source);
        }

        return (await genotypeSetService.GetPatientGenotypeSet(input, patientFrequencySet), source);
    }
}
