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

namespace Atlas.MatchPrediction.Services.MatchProbability;

public interface IGenotypeSetService
{
    Task<SubjectGenotypeSet> GetGenotypeSet(SubjectData subjectData, MatchPredictionParameters parameters);

    Task<SubjectGenotypeSet> GetPatientGenotypeSet(SingleDonorMatchProbabilityInput input);
}

internal class GenotypeSetService(
    IGenotypeImputationService genotypeImputer,
    IGenotypeConverter genotypeConverter,
    IHaplotypeFrequencyService haplotypeFrequencyService)
    : IGenotypeSetService
{
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
        await new MatchProbabilityNonDonorValidator().ValidateAndThrowAsync(input);

        var allowedLoci = LocusSettings.MatchPredictionLoci.Except(input.ExcludedLoci).ToHashSet();
        var patientFrequencySet = await haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(
            input.PatientFrequencySetMetadata ?? new FrequencySetMetadata()
        );

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

    private async Task<SubjectGenotypeSet> BuildGenotypeSet(SubjectData subjectData, MatchPredictionParameters parameters)
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
                GenotypeLikelihoods = imputedGenotypes.GenotypeLikelihoods,
                HfSetHlaNomenclatureVersion = preparedSubjectData.SubjectFrequencySet.FrequencySet.HlaNomenclatureVersion,
                MatchingAlgorithmHlaNomenclatureVersion = parameters.MatchingAlgorithmHlaNomenclatureVersion,
                SubjectLogDescription = preparedSubjectData.SubjectFrequencySet.SubjectLogDescription
            }
        );

        return new SubjectGenotypeSet(false, convertedGenotypes, imputedGenotypes.SumOfLikelihoods);
    }
}