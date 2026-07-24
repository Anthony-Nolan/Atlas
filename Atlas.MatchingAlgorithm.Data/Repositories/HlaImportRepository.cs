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
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;

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

            // The LociInfo(Func<>) ctor is eager (it invokes the factory for every locus in its constructor), so the entire
            // relation build - including the PhenotypeInfo / LociInfo allocations that are Finding #3 - is realised here on
            // the calling thread, NOT lazily during the insert below. Timed as CPU.
            LociInfo<IList<HlaNamePGroupRelation>> flattenedHlaToInsert;
            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_BuildHlaRelations)
                   ))
            {
                var hlaToInsert = donorsToImport.Select(donor => new PhenotypeInfo<IList<HlaNamePGroupRelation>>((locus, position) =>
                        donor?.MatchingHla?.GetPosition(locus, position)?.MatchingPGroups
                            .Select(pGroup =>
                                {
                                    var hlaName = donor.MatchingHla.GetPosition(locus, position).LookupName;
                                    var hlaNameId = hlaNameLookup.GetValueOrDefault(hlaName);
                                    return hlaName == null || processedHlaIds.GetLocus(locus).Contains(hlaNameId)
                                        ? null
                                        : new HlaNamePGroupRelation
                                        {
                                            HlaNameId = hlaNameId,
                                            PGroupId = pGroupLookup[pGroup]
                                        };
                                }
                            )
                            .Where(relation => relation != null)
                            .ToList()
                    )
                );

                flattenedHlaToInsert = new LociInfo<IList<HlaNamePGroupRelation>>(l =>
                    {
                        return hlaToInsert.SelectMany(h =>
                                {
                                    var position1Relations = h.GetPosition(l, LocusPosition.One) ?? new List<HlaNamePGroupRelation>();
                                    var position2Relations = h.GetPosition(l, LocusPosition.Two) ?? new List<HlaNamePGroupRelation>();
                                    return position1Relations.Concat(position2Relations);
                                }
                            )
                            .Where(x => x != null)
                            .Distinct()
                            .ToList();
                    }
                );
            }

            using (logger.TimeOperationAsMetric(
                       DataRefreshMetrics.DurationMsMetric,
                       DataRefreshMetrics.Dims(DataRefreshMetrics.Operation_InsertHlaRelations)
                   ))
            {
                await ImportHla(flattenedHlaToInsert);
            }

            return hlaNameLookup;
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

        private async Task ImportHla(LociInfo<IList<HlaNamePGroupRelation>> hlaNamesToImport)
        {
            // Distributed transactions are not yet supported in .Net core - see https://github.com/dotnet/runtime/issues/715
            // Until they are, we cannot update loci in parallel while also in a transaction scope. But if we are not in a transaction, it is quicker to run in parallel.
            // Therefore, we check for an open transaction here and either allow parallel execution across loci (via WhenAll), or do not (via WhenEach)
            var shouldRestrictParallelism = Transaction.Current != null;
            await hlaNamesToImport.WhenEachLocusWithOptionalParallelism(async (l, v) => await ImportHlaAtLocus(l, v), shouldRestrictParallelism);

            processedHlaIds = processedHlaIds.Map((l, existing) =>
                (ISet<int>)existing.Concat(hlaNamesToImport.GetLocus(l).Select(hla => hla.HlaNameId)).ToHashSet()
            );
        }

        private async Task ImportHlaAtLocus(Locus locus, IList<HlaNamePGroupRelation> hla)
        {
            // Use known new Hla strings to determine which relations to ignore! i.e. if not new, ignore it
            // This is safe as hla -> pgroup relation cannot change without a nomenclature change i.e. full data refresh
            await ImportProcessedHla(locus, hla);
        }

        private async Task ImportProcessedHla(Locus locus, IList<HlaNamePGroupRelation> newHlaRelations)
        {
            if (!newHlaRelations.Any() || !LocusSettings.MatchingOnlyLoci.Contains(locus))
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