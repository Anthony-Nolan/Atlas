using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IHlaImportRepository
    {
        /// <summary>
        /// Extracts all HLA information from a batch of donors to import, and runs HLA pre-processing on these alleles.
        /// If HLA processing has already been performed on an allele, it will not be performed again.
        /// </summary>
        /// <returns>
        /// A dictionary for quick lookup of the provided HLA.
        /// Key = HLA lookup name. Value = processed Atlas ID for this HLA. 
        /// </returns>
        Task<IDictionary<string, int>> ImportHla(IList<DonorInfoWithExpandedHla> donorsToImport);
    }

    /// <summary>
    /// The Atlas id lookups needed to build HLA relations. Named, rather than passed as two identically typed dictionaries,
    /// so that they cannot be transposed at a call site without the compiler noticing.
    /// </summary>
    internal record HlaImportLookups(IDictionary<string, int> HlaNames, IDictionary<string, int> PGroups);

    public class HlaImportRepository : Repository, IHlaImportRepository
    {
        private readonly IHlaNamesRepository hlaNamesRepository;
        private readonly IPGroupRepository pGroupRepository;
        private readonly IAtlasLogger logger;

        private LociInfo<ISet<int>> processedHlaIds;

        public HlaImportRepository(
            IHlaNamesRepository hlaNamesRepository,
            IPGroupRepository pGroupRepository,
            IConnectionStringProvider connectionStringProvider,
            IAtlasLogger logger) : base(connectionStringProvider)
        {
            this.hlaNamesRepository = hlaNamesRepository;
            this.pGroupRepository = pGroupRepository;
            this.logger = logger;
        }

        public async Task<IDictionary<string, int>> ImportHla(IList<DonorInfoWithExpandedHla> donorsToImport)
        {
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_EnsureProcessedHlaCache)
                   ))
            {
                await EnsureProcessedHlaCacheIsUpToDate();
            }

            // EnsureAll*Exist insert any brand-new names / p-groups and then re-read the WHOLE respective table to refresh
            // the in-memory id map. Per the spike profile (Finding #1) this per-batch full-table re-cache is the dominant
            // stage-50 cost, so each is timed on its own - both are DB-read bound and grow ~quadratically with table size.
            var pGroupLookup = await logger.RunTimedAsMetricAsync(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_EnsurePGroupsExist),
                () => pGroupRepository.EnsureAllPGroupsExist(donorsToImport.AllPGroupNames())
            );
            var hlaNameLookup = await logger.RunTimedAsMetricAsync(
                DataRefreshMetrics.DurationMsMetric,
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_EnsureHlaNamesExist),
                () => hlaNamesRepository.EnsureAllHlaNamesExist(donorsToImport.AllHlaNames())
            );

            // BuildHlaRelations traverses the whole batch synchronously, so the entire relation build is realised here on the
            // calling thread, NOT lazily during the insert below. Timed as CPU.
            LociInfo<ISet<HlaNamePGroupRelation>> hlaRelationsToInsert;
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BuildHlaRelations)
                   ))
            {
                var lookups = new HlaImportLookups(HlaNames: hlaNameLookup, PGroups: pGroupLookup);
                hlaRelationsToInsert = BuildHlaRelations(donorsToImport, lookups, processedHlaIds);
            }

            // The build's output is counted inside ImportHla, as HlaRelationsInserted - post ATL-280 the build produces
            // matching-loci relations only, so what it builds and what gets inserted are now the same set.
            // The two counters that used to sit here have gone with the build they measured: "candidates examined" existed
            // to expose the ~6x over-traversal of the old lazy-Select / eager-LociInfo build (the batch was walked once per
            // locus, across all twelve locus/position pairs, to insert only five loci), and "relations built" was its
            // counterpart output. ATL-280 replaced that with a single pass over the matching loci, so there is no longer a
            // cost-vs-output ratio to watch, and a separate built count would just duplicate the inserted one.
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_InsertHlaRelations)
                   ))
            {
                await ImportHla(hlaRelationsToInsert);
            }

            return hlaNameLookup;
        }

        /// <summary>
        /// Builds the HLA name to p-group relations that need inserting for a batch of donors, keyed by locus.
        /// Relations are not built for HLA names that have already been processed - as an HLA to p-group relation cannot
        /// change without a nomenclature change, i.e. a full data refresh.
        /// </summary>
        /// <remarks>
        /// The batch is traversed once, and relations are accumulated straight into the per-locus sets they will be inserted
        /// from, so duplicates - which are the norm, as HLA is heavily shared across donors - are discarded as they are found.
        /// A per-locus traversal would instead re-read every position of every donor once per locus, to consume two positions
        /// each time, allocating a throwaway relation list per position on every pass. Avoiding that churn is the point of the
        /// single pass, on a path that processes thousands of expanded donors per batch (see <c>HlaProcessor.BatchSize</c>).
        /// </remarks>
        internal static LociInfo<ISet<HlaNamePGroupRelation>> BuildHlaRelations(
            IList<DonorInfoWithExpandedHla> donorsToImport,
            HlaImportLookups lookups,
            LociInfo<ISet<int>> processedHlaIds)
        {
            // Relations outside the matching loci are discarded at insert time by ImportProcessedHla, so are not built at all.
            var loci = LocusSettings.MatchingOnlyLoci.ToList();
            var positions = EnumerateValues<LocusPosition>().ToList();
            var relationsByLocus = loci.ToDictionary(l => l, _ => new HashSet<HlaNamePGroupRelation>());

            foreach (var donor in donorsToImport)
            {
                var matchingHla = donor?.MatchingHla;
                if (matchingHla == null)
                {
                    continue;
                }

                foreach (var locus in loci)
                {
                    var relationsAtLocus = relationsByLocus[locus];
                    var processedAtLocus = processedHlaIds.GetLocus(locus);

                    foreach (var position in positions)
                    {
                        // Neither the HLA name, nor whether it has already been processed, depends on the p-group - so both are
                        // resolved once per (donor, locus, position), rather than once per candidate p-group. In particular, an
                        // already processed HLA name costs a single set lookup, instead of walking every one of its p-groups.
                        var hla = matchingHla.GetPosition(locus, position);
                        var hlaName = hla?.LookupName;
                        if (hlaName == null)
                        {
                            continue;
                        }

                        var hlaNameId = lookups.HlaNames.GetValueOrDefault(hlaName);
                        if (processedAtLocus.Contains(hlaNameId))
                        {
                            continue;
                        }

                        foreach (var pGroup in hla.MatchingPGroups)
                        {
                            relationsAtLocus.Add(new HlaNamePGroupRelation
                            {
                                HlaNameId = hlaNameId,
                                PGroupId = lookups.PGroups[pGroup]
                            });
                        }
                    }
                }
            }

            return new LociInfo<ISet<HlaNamePGroupRelation>>(
                l => relationsByLocus.TryGetValue(l, out var relations) ? relations : []
            );
        }

        private async Task EnsureProcessedHlaCacheIsUpToDate()
        {
            if (processedHlaIds == null)
            {
                await ForceProcessedHlaCacheGeneration();
            }
        }

        private async Task ForceProcessedHlaCacheGeneration()
        {
            processedHlaIds = new LociInfo<ISet<int>>(new HashSet<int>());

            // Distributed transactions are not yet supported in .Net core - see https://github.com/dotnet/runtime/issues/715
            // Until they are, we cannot update loci in parallel while also in a transaction scope. But if we are not in a transaction, it is quicker to run in parallel.
            // Therefore, we check for an open transaction here and either allow parallel execution across loci (via WhenAll), or do not (via WhenEach)
            var shouldRestrictParallelism = Transaction.Current != null;
            await new LociInfo<int>().WhenEachLocusWithOptionalParallelism(
                async (l, _) => { processedHlaIds = processedHlaIds.SetLocus(l, await GetExistingHlaAtLocus(l)); }, shouldRestrictParallelism
            );
        }

        /// <summary>
        /// Total relations across all loci. Post ATL-280 the build only produces relations for
        /// <see cref="LocusSettings.MatchingOnlyLoci"/>, which is exactly what <see cref="ImportProcessedHla"/> keeps.
        /// </summary>
        private static int CountRelationsToImport(LociInfo<ISet<HlaNamePGroupRelation>> relations) =>
            relations.Reduce((_, value, count) => count + (value?.Count ?? 0), 0);

        private async Task ImportHla(LociInfo<ISet<HlaNamePGroupRelation>> hlaNamesToImport)
        {
            // What the build above actually bought. Expected to collapse to ~0 within the first few hundred batches,
            // while BuildHlaRelations keeps costing the same - which is the whole of H11.
            logger.SendMetric(
                DataRefreshMetrics.CountMetric,
                CountRelationsToImport(hlaNamesToImport),
                DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_HlaRelationsInserted));

            // Distributed transactions are not yet supported in .Net core - see https://github.com/dotnet/runtime/issues/715
            // Until they are, we cannot update loci in parallel while also in a transaction scope. But if we are not in a transaction, it is quicker to run in parallel.
            // Therefore, we check for an open transaction here and either allow parallel execution across loci (via WhenAll), or do not (via WhenEach)
            var shouldRestrictParallelism = Transaction.Current != null;
            await hlaNamesToImport.WhenEachLocusWithOptionalParallelism(async (l, v) => await ImportHlaAtLocus(l, v), shouldRestrictParallelism);

            processedHlaIds = processedHlaIds.Map((l, existing) =>
                (ISet<int>)existing.Concat(hlaNamesToImport.GetLocus(l).Select(hla => hla.HlaNameId)).ToHashSet()
            );
        }

        private async Task ImportHlaAtLocus(Locus locus, ISet<HlaNamePGroupRelation> hla)
        {
            // Use known new Hla strings to determine which relations to ignore! i.e. if not new, ignore it
            // This is safe as hla -> pgroup relation cannot change without a nomenclature change i.e. full data refresh
            await ImportProcessedHla(locus, hla);
        }

        private async Task ImportProcessedHla(Locus locus, ISet<HlaNamePGroupRelation> newHlaRelations)
        {
            if (newHlaRelations.Count == 0 || !LocusSettings.MatchingOnlyLoci.Contains(locus))
            {
                return;
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.Id));
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.HlaNameId));
            dataTable.Columns.Add(nameof(HlaNamePGroupRelation.PGroupId));

            foreach (var relation in newHlaRelations)
            {
                dataTable.Rows.Add(0, relation.HlaNameId, relation.PGroupId);
            }

            using (var bulkCopy = new SqlBulkCopy(ConnectionStringProvider.GetConnectionString()))
            {
                bulkCopy.DestinationTableName = HlaNamePGroupRelation.TableName(locus);
                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }

        private async Task<ISet<int>> GetExistingHlaAtLocus(Locus locus)
        {
            if (!LocusSettings.MatchingOnlyLoci.Contains(locus))
            {
                return new HashSet<int>();
            }

            var sql = $@"SELECT DISTINCT h.{nameof(HlaNamePGroupRelation.HlaNameId)} FROM {HlaNamePGroupRelation.TableName(locus)} h";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return (await conn.QueryAsync<int>(sql)).ToHashSet();
            }
        }
    }
}