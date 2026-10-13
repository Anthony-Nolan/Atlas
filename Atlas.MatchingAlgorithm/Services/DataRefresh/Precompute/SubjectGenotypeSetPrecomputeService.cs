using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// One donor to precompute: the typing to impute, and the haplotype frequency set to impute it against.
/// </summary>
/// <remarks>
/// The frequency set arrives resolved, so choosing it - from the donor's registry and ethnicity - stays with the
/// caller, which is where the donor batch and its <c>RegistryCode</c> / <c>EthnicityCode</c> already are.
/// </remarks>
public sealed record PrecomputeSubject(int DonorId, PhenotypeInfo<string> HlaTyping, HaplotypeFrequencySet FrequencySet);

public interface ISubjectGenotypeSetPrecomputeService
{
    /// <summary>
    /// Computes, stores and assigns the genotype set of every given donor at all four allowed-loci combinations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Batch-shaped because its caller is: a donor import gives the donors that it wrote together in one call. Donors
    /// with the same typing and frequency set then pay for one imputation per combination.
    /// </para>
    ///
    /// <para>
    /// <b>All four rows of a donor, or none.</b> A donor gets its rows only when all four of its values are stored. When
    /// a value cannot be computed, the other donors still get their rows, and then the error is thrown. So the caller
    /// knows that some donors have no rows, and it can try them again: a value that is stored is found, and is not
    /// computed again.
    /// </para>
    /// </remarks>
    /// <param name="subjects">The donors to precompute.</param>
    /// <param name="matchingAlgorithmHlaNomenclatureVersion">
    /// The nomenclature version of <paramref name="targetDatabase"/>, which the P-group conversion runs at.
    /// </param>
    /// <param name="targetDatabase">
    /// The transient database the rows are written to: the dormant one during a data refresh, and whichever one a
    /// differential donor import writes its donors to.
    /// </param>
    /// <exception cref="Exception">
    /// When a donor has no rows: the error that stopped the computations, or else the error of the first value that
    /// failed.
    /// </exception>
    Task Precompute(
        IReadOnlyCollection<PrecomputeSubject> subjects,
        string matchingAlgorithmHlaNomenclatureVersion,
        TransientDatabase targetDatabase);
}

/// <summary>
/// Fills <c>DonorSubjectGenotypeSets</c> for the donors of a donor import, so that a search does not have to impute a
/// donor's genotype set again.
///
/// <para>
/// <b>It computes nothing itself.</b> <see cref="ISubjectGenotypeSetValueService"/> finds or computes the values, and
/// this class gives each donor the ids of its four values.
/// </para>
///
/// <para>
/// <b>Four imputations per donor.</b> The four combinations are nested, but their results are not derivable from one
/// another - every stage of imputation gates on the allowed loci, so restricting the locus set changes the answer
/// rather than trimming it. This is therefore four times the most expensive operation the system performs, per donor.
/// </para>
/// </summary>
public class SubjectGenotypeSetPrecomputeService : ISubjectGenotypeSetPrecomputeService
{
    private readonly ISubjectGenotypeSetValueService valueService;
    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;

    public SubjectGenotypeSetPrecomputeService(
        ISubjectGenotypeSetValueService valueService,
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory)
    {
        this.valueService = valueService;
        this.repositoryFactory = repositoryFactory;
    }

    /// <inheritdoc />
    public async Task Precompute(
        IReadOnlyCollection<PrecomputeSubject> subjects,
        string matchingAlgorithmHlaNomenclatureVersion,
        TransientDatabase targetDatabase)
    {
        if (subjects == null || subjects.Count == 0)
        {
            return;
        }

        var requests = subjects
            .SelectMany(subject => AllowedLociKeyExtensions.All.Select(allowedLociKey => (Subject: subject, AllowedLociKey: allowedLociKey)))
            .ToList();

        var results = await valueService.GetOrComputeValueIds(
            requests
                .Select(request => new SubjectGenotypeSetValueRequest(
                    request.Subject.HlaTyping,
                    request.AllowedLociKey,
                    request.Subject.FrequencySet,
                    $"donor {request.Subject.DonorId}"))
                .ToList(),
            matchingAlgorithmHlaNomenclatureVersion,
            targetDatabase);

        // One outcome per request, in the order of the requests: the four values of the first donor, then the next four.
        var assignments = requests
            .Zip(results.Outcomes, (request, outcome) => (request.Subject.DonorId, request.AllowedLociKey, outcome.ValueId))
            .Chunk(AllowedLociKeyExtensions.All.Count)
            .Where(donorValues => donorValues.All(value => value.ValueId != null))
            .SelectMany(donorValues => donorValues)
            .Select(value => new DonorSubjectGenotypeSetAssignment(value.DonorId, value.AllowedLociKey, value.ValueId.Value))
            .ToList();

        if (assignments.Count > 0)
        {
            await repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(targetDatabase).UpsertDonorAssignments(assignments);
        }

        ThrowIfAnyValueIsMissing(results);
    }

    /// <summary>
    /// When a value is missing, throws the error that stopped the computations, or else the error of the first value
    /// that failed.
    /// </summary>
    /// <remarks>
    /// It throws the original exception with its stack trace, so the caller logs the real error.
    /// </remarks>
    private static void ThrowIfAnyValueIsMissing(SubjectGenotypeSetValueResults results)
    {
        var missingValueCount = results.Outcomes.Count(outcome => outcome.ValueId == null);
        if (missingValueCount == 0)
        {
            return;
        }

        var error = results.StoppedBy
            ?? results.Outcomes.Select(outcome => outcome.Failure?.Exception).FirstOrDefault(exception => exception != null)
            // The value service gives a reason for each missing value, so this is a defect. Do not leave donors with no
            // rows and no error.
            ?? new InvalidOperationException($"{missingValueCount} genotype set values have no id and no failure.");

        ExceptionDispatchInfo.Capture(error).Throw();
    }
}
