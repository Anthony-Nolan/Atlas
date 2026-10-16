#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Validators.Debug;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

public interface IGenotypeSetDebugService
{
    /// <summary>
    /// Computes the genotype set of each subject at each requested locus combination, with the engine that the
    /// pre-computation and search use, and stores the sets when the request saves.
    /// </summary>
    /// <remarks>
    /// An error of one subject or of one genotype set is given in its own entry. It does not fail the request.
    /// </remarks>
    /// <exception cref="ValidationException">The request is not valid, or names a frequency set that does not exist.</exception>
    /// <exception cref="PrecomputeWritesNotAllowedException">The request saves, and saving is turned off.</exception>
    /// <exception cref="GenotypeSetDebugUnavailableException">No matching database is active.</exception>
    Task<GenotypeSetDebugResult> Compute(GenotypeSetDebugRequest request);
}

/// <summary>
/// The genotype set compute debug endpoint: the parts of a stored genotype set check that SQL cannot do. It imputes a
/// typing or donors live, gives the typing key of each set, and can store the sets.
/// </summary>
/// <remarks>
/// <para>
/// <b>The same value as the pre-computation.</b> Each set is computed with the steps of
/// <see cref="SubjectGenotypeSetValueService"/>: the same typing (empty names as null), key, engine, locus set,
/// nomenclature version and payload. So a set that this service stores is the set that the pre-computation would store.
/// </para>
///
/// <para>
/// <b>Saving writes through the repository, not through <see cref="ISubjectGenotypeSetValueService"/>.</b> The value
/// service computes each value that is not stored yet, and gives only the ids. Here the sets are already computed, and
/// the result tells which rows were stored before. The write is the same call that the value service makes,
/// <see cref="ISubjectGenotypeSetRepository.GetOrCreateValueIds"/>.
/// </para>
///
/// <para>
/// <b>Only the active database is written.</b> Data Refresh owns the dormant database. The active record is read again
/// just before the write, in case a refresh made the database dormant during the request.
/// </para>
/// </remarks>
internal class GenotypeSetDebugService : IGenotypeSetDebugService
{
    private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly IHaplotypeFrequencyLookupService frequencyLookupService;
    private readonly IHaplotypeFrequencyCache frequencyCache;
    private readonly IHaplotypeFrequencySetReader frequencySetReader;
    private readonly IGenotypeSetService genotypeSetService;
    private readonly DebugSettings debugSettings;
    private readonly ILogger<GenotypeSetDebugService> logger;

    public GenotypeSetDebugService(
        IDataRefreshHistoryRepository dataRefreshHistoryRepository,
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
        IHaplotypeFrequencyLookupService frequencyLookupService,
        IHaplotypeFrequencyCache frequencyCache,
        IHaplotypeFrequencySetReader frequencySetReader,
        IGenotypeSetService genotypeSetService,
        DebugSettings debugSettings,
        ILogger<GenotypeSetDebugService> logger)
    {
        this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        this.repositoryFactory = repositoryFactory;
        this.frequencyLookupService = frequencyLookupService;
        this.frequencyCache = frequencyCache;
        this.frequencySetReader = frequencySetReader;
        this.genotypeSetService = genotypeSetService;
        this.debugSettings = debugSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<GenotypeSetDebugResult> Compute(GenotypeSetDebugRequest request)
    {
        await new GenotypeSetDebugRequestValidator().ValidateAndThrowAsync(request);

        if (request.Save && !debugSettings.AllowPrecomputeWrites)
        {
            throw new PrecomputeWritesNotAllowedException();
        }

        var activeRecord = dataRefreshHistoryRepository.GetActiveRecord()
            ?? throw new GenotypeSetDebugUnavailableException("No data refresh has completed, so no matching database is active.");

        var requestedFrequencySet = await GetRequestedFrequencySet(request.HaplotypeFrequencySetId);
        IReadOnlyCollection<AllowedLociKey> allowedLociKeys = request.AllowedLociKeys is { Count: > 0 }
            ? request.AllowedLociKeys.Distinct().ToList()
            : AllowedLociKeyExtensions.All;

        var subjects = await GetSubjects(request, activeRecord.Database);

        // Donors with the same typing and frequency set have one key: each key is imputed once.
        var computations = new Dictionary<SubjectGenotypeSetKey, Computation>();
        var computed = new List<ComputedSubject>(subjects.Count);
        foreach (var subject in subjects)
        {
            computed.Add(await ComputeSubject(
                subject,
                requestedFrequencySet,
                allowedLociKeys,
                activeRecord.HlaNomenclatureVersion,
                request.IncludeGenotypes,
                computations));
        }

        if (request.Save)
        {
            computed = await Save(computed, activeRecord);
        }

        return new GenotypeSetDebugResult(
            activeRecord.Database,
            activeRecord.HlaNomenclatureVersion,
            computed.Select(subject => subject.ToResult()).ToList());
    }

    /// <summary>The set that the request names, if it names one, and whether search would choose it now.</summary>
    private async Task<FrequencySetChoice?> GetRequestedFrequencySet(int? id)
    {
        if (id == null)
        {
            return null;
        }

        var set = await frequencySetReader.GetHaplotypeFrequencySet(id.Value)
            ?? throw new ValidationException([
                new ValidationFailure(nameof(GenotypeSetDebugRequest.HaplotypeFrequencySetId), $"Haplotype frequency set {id} does not exist.")
            ]);

        // Search chooses the active set of the exact registry and ethnicity first, so the set is the one search would choose
        // for its own codes exactly when it is that active set.
        var activeSets = await frequencyCache.GetActiveHaplotypeFrequencySets();
        var isActive = activeSets.TryGetValue((set.RegistryCode, set.EthnicityCode), out var activeSet) && activeSet.Id == set.Id;

        return new FrequencySetChoice(set, isActive);
    }

    private async Task<IReadOnlyList<SubjectToCompute>> GetSubjects(GenotypeSetDebugRequest request, TransientDatabase database)
    {
        if (request.HlaTyping is not null)
        {
            return [new SubjectToCompute(null, null, request.HlaTyping, request.FrequencySetMetadata ?? new FrequencySetMetadata())];
        }

        // The database compares the codes without regard to case, so the codes are matched here the same way.
        var codes = request.ExternalDonorCodes!.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var donors = await repositoryFactory.GetDonorInspectionRepositoryForDatabase(database).GetDonorsByExternalDonorCodes(codes);
        var donorsByCode = donors.ToLookup(donor => donor.ExternalDonorCode, StringComparer.OrdinalIgnoreCase);

        return codes
            .SelectMany(code => donorsByCode.Contains(code)
                ? donorsByCode[code].Select(donor => new SubjectToCompute(
                    donor.ExternalDonorCode,
                    donor,
                    donor.HlaNames,
                    new FrequencySetMetadata { RegistryCode = donor.RegistryCode, EthnicityCode = donor.EthnicityCode }))
                : [new SubjectToCompute(code, null, null, null)])
            .ToList();
    }

    private async Task<ComputedSubject> ComputeSubject(
        SubjectToCompute subject,
        FrequencySetChoice? requestedFrequencySet,
        IReadOnlyCollection<AllowedLociKey> allowedLociKeys,
        string hlaNomenclatureVersion,
        bool includeGenotypes,
        IDictionary<SubjectGenotypeSetKey, Computation> computations)
    {
        var result = new GenotypeSetDebugSubject
        {
            ExternalDonorCode = subject.ExternalDonorCode,
            DonorId = subject.Donor?.DonorId,
            Found = subject.HlaTyping is not null,
            IsAvailableForSearch = subject.Donor?.IsAvailableForSearch,
            RegistryCode = subject.Donor?.RegistryCode,
            EthnicityCode = subject.Donor?.EthnicityCode,
            HlaTyping = subject.HlaTyping
        };

        if (subject.HlaTyping is null)
        {
            return new ComputedSubject(result, subject.Donor, []);
        }

        FrequencySetChoice frequencySet;
        try
        {
            // The rules of search: registry and ethnicity, then registry, then the global set.
            frequencySet = requestedFrequencySet
                ?? new FrequencySetChoice(await frequencyLookupService.GetSingleHaplotypeFrequencySet(subject.FrequencySetMetadata), true);
        }
        catch (Exception exception)
        {
            return new ComputedSubject(result with { Error = Describe(exception) }, subject.Donor, []);
        }

        // As the pre-computation does: the key reads null and empty names as one untyped position, but imputation sends an
        // empty name to the HLA Metadata Dictionary, which throws.
        var typing = subject.HlaTyping.Map(hla => string.IsNullOrEmpty(hla) ? null : hla);
        var description = subject.ExternalDonorCode == null ? "debug typing" : $"debug donor {subject.ExternalDonorCode}";
        var frequencySetUsed = frequencySet.ToResult();

        var items = new List<ComputedItem>(allowedLociKeys.Count);
        foreach (var allowedLociKey in allowedLociKeys)
        {
            var key = new SubjectGenotypeSetKey(
                SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(typing, allowedLociKey),
                frequencySet.Set.Id,
                allowedLociKey);

            if (!computations.TryGetValue(key, out var computation))
            {
                computation = await ComputeValue(typing, frequencySet.Set, allowedLociKey, hlaNomenclatureVersion, description, includeGenotypes);
                computations[key] = computation;
            }

            items.Add(new ComputedItem(key, computation, frequencySetUsed));
        }

        return new ComputedSubject(result, subject.Donor, items);
    }

    /// <summary>
    /// The steps of <see cref="SubjectGenotypeSetValueService"/>, so that the set and the payload are what the
    /// pre-computation stores for the same typing, frequency set, locus combination and nomenclature version.
    /// </summary>
    private async Task<Computation> ComputeValue(
        PhenotypeInfo<string?> typing,
        HaplotypeFrequencySet frequencySet,
        AllowedLociKey allowedLociKey,
        string hlaNomenclatureVersion,
        string description,
        bool includeGenotypes)
    {
        try
        {
            var subjectData = new SubjectData(typing, new SubjectFrequencySet(frequencySet, description));
            var parameters = new MatchPredictionParameters(allowedLociKey.ToLoci().ToHashSet(), hlaNomenclatureVersion);

            var genotypeSet = await genotypeSetService.GetGenotypeSet(subjectData, parameters);

            return new Computation(
                genotypeSet.IsUnrepresented,
                genotypeSet.Genotypes?.Count ?? 0,
                genotypeSet.SumOfLikelihoods,
                genotypeSet.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(genotypeSet),
                includeGenotypes ? genotypeSet.Genotypes?.ToList() : null,
                null);
        }
        catch (Exception exception)
        {
            return new Computation(false, 0, 0m, null, null, Describe(exception));
        }
    }

    private async Task<List<ComputedSubject>> Save(List<ComputedSubject> subjects, ActiveDataRefreshRecord activeRecord)
    {
        var valuesToStore = subjects
            .SelectMany(subject => subject.Items)
            .Where(item => item.Computation.Error == null)
            .Select(item => new SubjectGenotypeSetValueToStore(item.Key, item.Computation.IsUnrepresented, item.Computation.Payload))
            .DistinctBy(value => value.Key)
            .ToList();

        if (valuesToStore.Count == 0)
        {
            return subjects;
        }

        if (dataRefreshHistoryRepository.GetActiveRecord()?.Id != activeRecord.Id)
        {
            return WithErrorOnSavingSubjects(subjects, "The active database changed during the request, so nothing was saved.");
        }

        try
        {
            var repository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
            var keys = valuesToStore.Select(value => value.Key).ToList();

            // Read before the write only to tell the two outcomes apart: a concurrent writer can store a row in between.
            var existingIds = await repository.GetExistingValueIds(keys);
            var ids = await repository.GetOrCreateValueIds(valuesToStore);

            var saved = subjects.Select(subject => subject.WithSavedValues(ids, existingIds)).ToList();

            // As the pre-computation does, a donor gets its link rows only when all its requested values are stored.
            // The upsert replaces the donor's rows for these combinations, also for a frequency set that search would
            // not choose.
            var linkedDonors = saved.Where(subject => subject.CanBeLinked).ToList();
            var assignments = linkedDonors
                .SelectMany(subject => subject.Items.Select(item => new DonorSubjectGenotypeSetAssignment(
                    subject.Donor!.DonorId,
                    item.Key.AllowedLociKey,
                    item.Saved!.SubjectGenotypeSetValueId)))
                .ToList();

            if (assignments.Count > 0)
            {
                await repository.UpsertDonorAssignments(assignments);
            }

            logger.LogInformation(
                "Genotype set debug endpoint saved to {Database}: {CreatedValueCount} value rows created, {ExistingValueCount} found, "
                + "link rows written for {LinkedDonorCount} donors. Donors: {ExternalDonorCodes}. Keys: {HlaTypingKeys}",
                activeRecord.Database,
                keys.Count(key => !existingIds.ContainsKey(key)),
                keys.Count(existingIds.ContainsKey),
                linkedDonors.Count,
                string.Join(",", subjects.Select(subject => subject.Subject.ExternalDonorCode).Where(code => code != null)),
                string.Join(",", keys.Select(key => key.HlaTypingKey).Distinct()));

            return saved
                .Select(subject => subject.Donor == null ? subject : subject.WithLinkRowsWritten(linkedDonors.Contains(subject)))
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Genotype set debug endpoint failed to save to {Database}", activeRecord.Database);
            return WithErrorOnSavingSubjects(subjects, $"Saving failed: {Describe(exception)}");
        }
    }

    private static List<ComputedSubject> WithErrorOnSavingSubjects(IEnumerable<ComputedSubject> subjects, string error) =>
        subjects
            .Select(subject => subject.Items.Any(item => item.Computation.Error == null)
                ? subject with { Subject = subject.Subject with { Error = error } }
                : subject)
            .ToList();

    private static string Describe(Exception exception) => $"{exception.GetType().Name}: {exception.Message}";

    /// <param name="ExternalDonorCode">Null for a typing.</param>
    /// <param name="Donor">Null for a typing, and for a donor code that no donor has.</param>
    /// <param name="HlaTyping">Null for a donor code that no donor has.</param>
    /// <param name="FrequencySetMetadata">The codes that choose the frequency set; null for a donor code that no donor has.</param>
    private sealed record SubjectToCompute(
        string? ExternalDonorCode,
        DonorInfo? Donor,
        PhenotypeInfo<string>? HlaTyping,
        FrequencySetMetadata? FrequencySetMetadata);

    private sealed record FrequencySetChoice(HaplotypeFrequencySet Set, bool IsActive)
    {
        public HaplotypeFrequencySetUsed ToResult() =>
            new(Set.Id, Set.Name, Set.RegistryCode, Set.EthnicityCode, Set.HlaNomenclatureVersion, IsActive);
    }

    /// <summary>One imputation, shared by the subjects whose key is the same.</summary>
    /// <param name="Payload">The encoded set; null when the set is unrepresented, as for the pre-computation.</param>
    /// <param name="Genotypes">Kept only when the request includes genotypes.</param>
    /// <param name="Error">Why the set could not be computed; null when it was computed.</param>
    private sealed record Computation(
        bool IsUnrepresented,
        int GenotypeCount,
        decimal SumOfLikelihoods,
        byte[]? Payload,
        IReadOnlyCollection<GenotypeAtDesiredResolutions>? Genotypes,
        string? Error);

    private sealed record ComputedItem(
        SubjectGenotypeSetKey Key,
        Computation Computation,
        HaplotypeFrequencySetUsed FrequencySet,
        SavedGenotypeSetValue? Saved = null)
    {
        public GenotypeSetDebugItem ToResult() => new()
        {
            AllowedLociKey = Key.AllowedLociKey,
            HlaTypingKey = Key.HlaTypingKey,
            HaplotypeFrequencySet = FrequencySet,
            IsUnrepresented = Computation.IsUnrepresented,
            GenotypeCount = Computation.GenotypeCount,
            SumOfLikelihoods = Computation.SumOfLikelihoods,
            PayloadSizeBytes = Computation.Payload?.Length,
            Genotypes = Computation.Genotypes,
            Saved = Saved,
            Error = Computation.Error
        };
    }

    private sealed record ComputedSubject(GenotypeSetDebugSubject Subject, DonorInfo? Donor, IReadOnlyList<ComputedItem> Items)
    {
        /// <summary>A donor whose every requested value is stored.</summary>
        public bool CanBeLinked => Donor != null && Items.Count > 0 && Items.All(item => item.Saved != null);

        public ComputedSubject WithSavedValues(
            IReadOnlyDictionary<SubjectGenotypeSetKey, int> ids,
            IReadOnlyDictionary<SubjectGenotypeSetKey, int> existingIds) =>
            this with
            {
                Items = Items
                    .Select(item => item.Computation.Error == null && ids.TryGetValue(item.Key, out var id)
                        ? item with
                        {
                            Saved = new SavedGenotypeSetValue(
                                id,
                                existingIds.ContainsKey(item.Key) ? SavedValueOutcome.AlreadyExisted : SavedValueOutcome.Created)
                        }
                        : item)
                    .ToList()
            };

        public ComputedSubject WithLinkRowsWritten(bool linkRowsWritten) =>
            this with { Subject = Subject with { LinkRowsWritten = linkRowsWritten } };

        public GenotypeSetDebugSubject ToResult() =>
            Subject with { GenotypeSets = Items.Select(item => item.ToResult()).ToList() };
    }
}
