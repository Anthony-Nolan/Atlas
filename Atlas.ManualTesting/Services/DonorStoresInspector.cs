using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.ManualTesting.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using MoreLinq.Extensions;

namespace Atlas.ManualTesting.Services
{
    public interface IDonorStoresInspector
    {
        /// <summary>
        /// Checks the active matching database for missing donors against the donor import database.
        /// </summary>
        /// <returns></returns>
        Task<MissingDonors> GetDonorsMissingFromActiveMatchingDatabase();
    }

    internal class DonorStoresInspector : IDonorStoresInspector
    {
        private readonly IDonorReadRepository donorImportRepository;
        private readonly IDonorInspectionRepository activeMatchingRepository;
        private const int BatchSize = 10000;

        public DonorStoresInspector(
            IDonorReadRepository donorImportRepository,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IActiveMatchingDatabaseConnectionStringProvider connectionStringProvider)
        {
            this.donorImportRepository = donorImportRepository;
            activeMatchingRepository = new DonorInspectionRepository(connectionStringProvider);
        }

        /// <inheritdoc />
        public async Task<MissingDonors> GetDonorsMissingFromActiveMatchingDatabase()
        {
            var missingDonors = new List<Donor>();

            var donorsStream = donorImportRepository.StreamAllDonors();
            foreach (var streamedDonorBatch in donorsStream.Batch(BatchSize))
            {
                var donorImportDonors = streamedDonorBatch.ToList();
                var matchingDatabaseDonors = (await GetExistingDonorIdsFromActiveMatchingDatabase(donorImportDonors.Select(d => d.AtlasId))).ToList();

                var missing = donorImportDonors.Where(d => !matchingDatabaseDonors.Contains(d.AtlasId)).ToList();
                if (!missing.Any())
                {
                    continue;
                }

                missingDonors.AddRange(missing);
            }

            return BuildMissingDonors(missingDonors);
        }

        private async Task<IEnumerable<int>> GetExistingDonorIdsFromActiveMatchingDatabase(IEnumerable<int> donors)
        {
            var existingDonors = await activeMatchingRepository.GetDonors(donors);
            return existingDonors.Keys;
        }

        private static MissingDonors BuildMissingDonors(IEnumerable<Donor> donors)
        {
            var missingDonors = donors.Select(BuildMissingDonor).ToList();

            return new MissingDonors
            {
                Count = missingDonors.Count,
                Donors = missingDonors,
                DonorTypeCounts = missingDonors.GroupDonorsBy(d => d.DonorType),
                RegistryCounts = missingDonors.GroupDonorsBy(d => d.RegistryCode),
                UpdateFileCounts = missingDonors.GroupDonorsBy(d => d.UpdateFile),
                LastUpdatedCounts = missingDonors.GroupDonorsBy(d => d.LastUpdated.ToString("yyyy/MM/dd"))
            };
        }

        private static MissingDonor BuildMissingDonor(Donor d)
        {
            return new MissingDonor
            {
                AtlasId = d.AtlasId,
                ExternalDonorCode = d.ExternalDonorCode,
                DonorType = d.DonorType.ToString(),
                RegistryCode = d.RegistryCode,
                UpdateFile = d.UpdateFile,
                LastUpdated = d.LastUpdated
            };
        }


    }
}