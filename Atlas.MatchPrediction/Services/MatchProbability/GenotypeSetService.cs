using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Services.MatchProbability;

public interface IGenotypeSetService
{
    Task<SubjectGenotypeSet> GetGenotypeSet(SubjectData subjectData, MatchPredictionParameters parameters);

    /// <summary>
    /// True when the subject's typing has exactly one small g group at both positions of every allowed locus. Its
    /// genotype set is then the one certain genotype, which <see cref="GetGenotypeSet"/> builds without the haplotype
    /// frequency set, so it is cheap to compute live.
    /// </summary>
    /// <remarks>
    /// The rule of which genotype sets are not precomputed (ATL-478). The data refresh, the donor import and search all
    /// ask it here, and it is the same test that imputation uses to skip the frequency set, so they cannot disagree.
    /// The typing is prepared as for <see cref="GetGenotypeSet"/>. It reads the HLA Metadata Dictionary, not the
    /// frequency set: the set gives only the nomenclature version that the typing is converted at.
    /// </remarks>
    Task<bool> IsUnambiguousAtSmallGGroup(SubjectData subjectData, MatchPredictionParameters parameters);

    Task<SubjectGenotypeSet> GetPatientGenotypeSet(SingleDonorMatchProbabilityInput input);

    /// <summary>
    /// As <see cref="GetPatientGenotypeSet(SingleDonorMatchProbabilityInput)"/>, with the patient frequency set already
    /// resolved by the caller - so that a caller that also needs the set's id (for a stored set's key) uses the same set.
    /// </summary>
    Task<SubjectGenotypeSet> GetPatientGenotypeSet(SingleDonorMatchProbabilityInput input, HaplotypeFrequencySet patientFrequencySet);
}

internal class GenotypeSetService(
    IGenotypeImputationService genotypeImputer,
    IGenotypeConverter genotypeConverter,
    IHaplotypeFrequencyLookupService haplotypeFrequencyService)
    : IGenotypeSetService
{
    /// <summary>One instance for every call: see <c>MatchProbabilityService.InputValidator</c> (ATL-476).</summary>
    private static readonly MatchProbabilityNonDonorValidator NonDonorValidator = new();

    private static readonly IReadOnlyDictionary<int, Dictionary<(Locus, string), string>> LocusValueReplacementMapping =
        new Dictionary<int, Dictionary<(Locus, string), string>>
        {
            {
                3590, new Dictionary<(Locus, string), string>
                {
                    { (Locus.C, "*04:09N"), "*04:09L" },
                    { (Locus.C, "04:09N"), "04:09L" },
                }
            }
        };

    public async Task<SubjectGenotypeSet> GetPatientGenotypeSet(SingleDonorMatchProbabilityInput input)
    {
        await NonDonorValidator.ValidateAndThrowAsync(input);

        var patientFrequencySet = await haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(
            input.PatientFrequencySetMetadata ?? new FrequencySetMetadata()
        );

        return await BuildPatientGenotypeSet(input, patientFrequencySet);
    }

    public async Task<SubjectGenotypeSet> GetPatientGenotypeSet(SingleDonorMatchProbabilityInput input, HaplotypeFrequencySet patientFrequencySet)
    {
        await NonDonorValidator.ValidateAndThrowAsync(input);

        return await BuildPatientGenotypeSet(input, patientFrequencySet);
    }

    private async Task<SubjectGenotypeSet> BuildPatientGenotypeSet(SingleDonorMatchProbabilityInput input, HaplotypeFrequencySet patientFrequencySet)
    {
        var allowedLoci = LocusSettings.MatchPredictionLoci.Except(input.ExcludedLoci).ToHashSet();

        return await BuildGenotypeSet(
            new SubjectData(
                input.PatientHla.ToPhenotypeInfo(),
                new SubjectFrequencySet(patientFrequencySet, "patient")
            ),
            new MatchPredictionParameters(allowedLoci, input.MatchingAlgorithmHlaNomenclatureVersion)
        );
    }

    public async Task<SubjectGenotypeSet> GetGenotypeSet(SubjectData subjectData, MatchPredictionParameters parameters)
    {
        return await BuildGenotypeSet(subjectData, parameters);
    }

    public async Task<bool> IsUnambiguousAtSmallGGroup(SubjectData subjectData, MatchPredictionParameters parameters)
    {
        return await genotypeImputer.IsUnambiguousAtSmallGGroup(new ImputationInput
        {
            SubjectData = PrepareSubjectData(subjectData, parameters),
            MatchPredictionParameters = parameters
        });
    }

    private async Task<SubjectGenotypeSet> BuildGenotypeSet(SubjectData subjectData, MatchPredictionParameters parameters)
    {
        var preparedSubjectData = PrepareSubjectData(subjectData, parameters);

        var imputedGenotypes = await genotypeImputer.Impute(new ImputationInput
            {
                SubjectData = preparedSubjectData,
                MatchPredictionParameters = parameters
            }
        );

        if (imputedGenotypes.Genotypes.IsNullOrEmpty())
        {
            return new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m);
        }

        var convertedGenotypes = await genotypeConverter.ConvertGenotypesForMatchCalculation(new GenotypeConverterInput
            {
                CompressedPhenotype = preparedSubjectData.HlaTyping,
                AllowedLoci = parameters.AllowedLoci,
                Genotypes = imputedGenotypes.Genotypes,
                HfSetHlaNomenclatureVersion = preparedSubjectData.SubjectFrequencySet.FrequencySet.HlaNomenclatureVersion,
                MatchingAlgorithmHlaNomenclatureVersion = parameters.MatchingAlgorithmHlaNomenclatureVersion,
                SubjectLogDescription = preparedSubjectData.SubjectFrequencySet.SubjectLogDescription
            }
        );

        return new SubjectGenotypeSet(false, convertedGenotypes, imputedGenotypes.SumOfLikelihoods);
    }

    /// <summary>The typing with the replacements of the matching nomenclature version applied.</summary>
    private static SubjectData PrepareSubjectData(SubjectData subjectData, MatchPredictionParameters parameters)
    {
        int? matchingAlgorithmHlaNomenclatureVersion = null;
        if (parameters.MatchingAlgorithmHlaNomenclatureVersion != null)
        {
            if (!int.TryParse(parameters.MatchingAlgorithmHlaNomenclatureVersion, out var parsedVersion))
            {
                throw new ArgumentException(
                    $"MatchingAlgorithmHlaNomenclatureVersion must be a valid integer, but was '{parameters.MatchingAlgorithmHlaNomenclatureVersion}'.",
                    nameof(parameters));
            }
            matchingAlgorithmHlaNomenclatureVersion = parsedVersion;
        }

        SubjectData preparedSubjectData;

        if (matchingAlgorithmHlaNomenclatureVersion == null)
        {
            preparedSubjectData = subjectData;
        }
        else
        {
            var matchingReplacementMappingKeys =
                LocusValueReplacementMapping.Keys
                    .Where(k => k <= matchingAlgorithmHlaNomenclatureVersion)
                    .ToList();

            preparedSubjectData = new SubjectData(
                subjectData.HlaTyping.Map((locus, _, hla) =>
                    {
                        string replacement = null;
                        foreach (var matchingReplacementMappingKey in matchingReplacementMappingKeys)
                        {
                            LocusValueReplacementMapping[matchingReplacementMappingKey].TryGetValue((locus, hla), out replacement);
                        }

                        return replacement ?? hla;
                    }
                ),
                subjectData.SubjectFrequencySet
            );
        }

        return preparedSubjectData;
    }
}