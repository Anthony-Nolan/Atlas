using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    /// <summary>
    /// Precomputes the genotype sets of donors that a differential donor import has just written.
    /// </summary>
    public interface IDonorGenotypeSetPrecomputer
    {
        /// <summary>
        /// Computes, stores and assigns the genotype sets of <paramref name="donors"/> in <paramref name="targetDatabase"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Best effort: this never throws.</b> A failure is logged, and the donors it affects are left with no
        /// <c>DonorSubjectGenotypeSets</c> rows, so search imputes them live. The donor import has already succeeded,
        /// and its result must not depend on this step - the same rule as a failed precompute batch in a data refresh.
        /// </para>
        ///
        /// <para>
        /// Call it only after the donor transaction has committed. Its writes run outside any ambient transaction, and
        /// would wait on the locks of an uncommitted one.
        /// </para>
        /// </remarks>
        /// <param name="donors">Donors whose HLA was written in <paramref name="targetDatabase"/>.</param>
        /// <param name="targetDatabase">The transient database the donors were written to.</param>
        /// <param name="hlaNomenclatureVersion">The HLA nomenclature version the donors' HLA was processed at.</param>
        Task PrecomputeBestEffort(IReadOnlyCollection<DonorInfo> donors, TransientDatabase targetDatabase, string hlaNomenclatureVersion);
    }

    /// <summary>
    /// Used by hosts that do not run the Match Prediction genotype set pipeline: today the matching algorithm API. The donor
    /// service still deletes an updated donor's old rows, so these donors have no rows and search imputes them live.
    /// </summary>
    public class NoOpDonorGenotypeSetPrecomputer : IDonorGenotypeSetPrecomputer
    {
        public Task PrecomputeBestEffort(IReadOnlyCollection<DonorInfo> donors, TransientDatabase targetDatabase, string hlaNomenclatureVersion) =>
            Task.CompletedTask;
    }

    /// <inheritdoc />
    public class DonorGenotypeSetPrecomputer : IDonorGenotypeSetPrecomputer
    {
        internal const string FailureEventName = "Donor import precompute failed";

        /// <summary>Donor ids listed in the failure event. The count is always the full number.</summary>
        internal const int MaxLoggedDonorIds = 100;

        /// <summary>Exceptions logged per batch. When many groups fail, they almost always fail for the same reason.</summary>
        internal const int MaxLoggedExceptions = 10;

        private readonly IHaplotypeFrequencyLookupService haplotypeFrequencyLookupService;
        private readonly ISubjectGenotypeSetPrecomputeService precomputeService;
        private readonly IMatchingAlgorithmImportLogger logger;

        public DonorGenotypeSetPrecomputer(
            IHaplotypeFrequencyLookupService haplotypeFrequencyLookupService,
            ISubjectGenotypeSetPrecomputeService precomputeService,
            IMatchingAlgorithmImportLogger logger)
        {
            this.haplotypeFrequencyLookupService = haplotypeFrequencyLookupService;
            this.precomputeService = precomputeService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task PrecomputeBestEffort(IReadOnlyCollection<DonorInfo> donors, TransientDatabase targetDatabase, string hlaNomenclatureVersion)
        {
            if (donors == null || donors.Count == 0)
            {
                return;
            }

            var failures = new Failures();

            try
            {
                // No caller should call this inside a transaction, but if one did, the writes would join it - so a
                // failure here could roll back donor data. Suppressing keeps that impossible.
                using var scope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);

                var subjects = await ToSubjects(donors, failures);
                await Precompute(subjects, targetDatabase, hlaNomenclatureVersion, failures);

                scope.Complete();
            }
            catch (Exception exception)
            {
                // Only an unexpected error in this class reaches here: each step catches its own. Which donors got
                // their rows is then unknown, so the whole batch is reported.
                failures.Add(exception, donors.Select(d => d.DonorId));
            }

            LogFailures(failures, targetDatabase);
        }

        /// <summary>
        /// Resolves each donor's haplotype frequency set, once per distinct (registry, ethnicity). A donor whose set
        /// cannot be resolved is a failure, and is not precomputed.
        /// </summary>
        private async Task<List<PrecomputeSubject>> ToSubjects(IReadOnlyCollection<DonorInfo> donors, Failures failures)
        {
            var subjects = new List<PrecomputeSubject>(donors.Count);

            foreach (var donorsWithMetadata in donors.GroupBy(d => (d.RegistryCode, d.EthnicityCode)))
            {
                HaplotypeFrequencySet frequencySet;
                try
                {
                    frequencySet = await haplotypeFrequencyLookupService.GetSingleHaplotypeFrequencySet(new FrequencySetMetadata
                    {
                        RegistryCode = donorsWithMetadata.Key.RegistryCode,
                        EthnicityCode = donorsWithMetadata.Key.EthnicityCode
                    });
                }
                catch (Exception exception)
                {
                    failures.Add(exception, donorsWithMetadata.Select(d => d.DonorId));
                    continue;
                }

                subjects.AddRange(donorsWithMetadata.Select(d => new PrecomputeSubject(d.DonorId, d.HlaNames, frequencySet)));
            }

            return subjects;
        }

        /// <summary>
        /// Precomputes the whole batch in one call. If that fails, tries again once per distinct (typing, frequency set),
        /// so one typing that cannot be imputed does not leave the whole batch without rows.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Donors that share a typing and frequency set are computed once: the precompute service de-duplicates by the
        /// stored key. So does each group here, since a group is exactly the donors that share it.
        /// </para>
        ///
        /// <para>
        /// Trying again is safe. Values the failed call stored are found and reused, not stored twice, and donor
        /// assignments are an upsert.
        /// </para>
        /// </remarks>
        private async Task Precompute(
            IReadOnlyCollection<PrecomputeSubject> subjects,
            TransientDatabase targetDatabase,
            string hlaNomenclatureVersion,
            Failures failures)
        {
            if (subjects.Count == 0)
            {
                return;
            }

            try
            {
                await precomputeService.Precompute(subjects, hlaNomenclatureVersion, targetDatabase);
                return;
            }
            catch (Exception exception)
            {
                failures.AddBatchFailure(exception);
            }

            var groups = subjects
                .GroupBy(s => (SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(s.HlaTyping, AllowedLociKey.ABCDrb1Dqb1), s.FrequencySet.Id))
                .ToList();

            // One group is the whole batch, and it has just failed.
            if (groups.Count == 1)
            {
                failures.Add(null, subjects.Select(s => s.DonorId));
                return;
            }

            foreach (var group in groups)
            {
                var groupSubjects = group.ToList();
                try
                {
                    await precomputeService.Precompute(groupSubjects, hlaNomenclatureVersion, targetDatabase);
                }
                catch (Exception exception)
                {
                    failures.Add(exception, groupSubjects.Select(s => s.DonorId));
                }
            }
        }

        private void LogFailures(Failures failures, TransientDatabase targetDatabase)
        {
            if (!failures.Any)
            {
                return;
            }

            try
            {
                var failedDonorIds = failures.DonorIds.Distinct().OrderBy(id => id).ToList();
                var firstException = failures.Exceptions.First();

                logger.SendEvent(FailureEventName, LogLevel.Warn, new Dictionary<string, string>
                {
                    { "FailedDonorCount", failedDonorIds.Count.ToString() },
                    { "FailedDonorIds", string.Join(",", failedDonorIds.Take(MaxLoggedDonorIds)) },
                    { "TargetDatabase", targetDatabase.GetStringValue() },
                    { "ExceptionType", firstException.GetType().FullName },
                    { "ExceptionMessage", firstException.Message },
                    { "ExceptionCount", failures.Exceptions.Count.ToString() },
                });

                foreach (var exception in failures.Exceptions.Take(MaxLoggedExceptions))
                {
                    logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
                    {
                        { "EventName", FailureEventName },
                        { "TargetDatabase", targetDatabase.GetStringValue() },
                    });
                }
            }
            catch
            {
                // Logging must not fail the import either.
            }
        }

        private sealed class Failures
        {
            public List<Exception> Exceptions { get; } = new();
            public List<int> DonorIds { get; } = new();
            public bool Any => DonorIds.Count > 0;

            /// <summary>A failure of the whole-batch call. Its donors are counted by the per-group calls that follow.</summary>
            public void AddBatchFailure(Exception exception) => Exceptions.Add(exception);

            public void Add(Exception exception, IEnumerable<int> donorIds)
            {
                if (exception != null)
                {
                    Exceptions.Add(exception);
                }

                DonorIds.AddRange(donorIds);
            }
        }
    }
}
