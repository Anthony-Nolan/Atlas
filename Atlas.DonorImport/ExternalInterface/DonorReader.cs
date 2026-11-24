using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.Mapping;

namespace Atlas.DonorImport.ExternalInterface
{
    public interface IDonorReader
    {
        IEnumerable<Donor> StreamAllDonors();

        /// <summary>
        /// Fetch donors by internal *ATLAS IDs*
        /// </summary>
        /// <param name="donorIds">Atlas IDs to fetch</param>
        /// <returns>A dictionary of Donor ID to full donor information.</returns>
        Task<IReadOnlyDictionary<int, Donor>> GetDonors(IEnumerable<int> donorIds);

        /// <summary>
        /// Finds which of the given *external* donor codes belong to donors in the donor store.
        /// Reads only the codes, not the donor rows.
        /// </summary>
        /// <param name="externalDonorCodes">External Donor Codes to look for</param>
        /// <returns>The given codes that belong to donors in the donor store.</returns>
        Task<IReadOnlyCollection<string>> GetExistingExternalDonorCodes(IEnumerable<string> externalDonorCodes);

        /// <summary>
        /// Fetch donors added/updated since a certain date
        /// </summary>
        /// <param name="cutoffDate">Date past which donor must have been updated to be considered</param>
        /// <returns>A dictionary of External Donor Code to Atlas donor id.</returns>
        Task<IReadOnlyDictionary<string, int>> GetDonorIdsUpdatedSince(DateTimeOffset cutoffDate);
    }

    public class DonorReader : IDonorReader
    {
        private readonly IDonorReadRepository donorReadRepository;

        public DonorReader(IDonorReadRepository donorReadRepository)
        {
            this.donorReadRepository = donorReadRepository;
        }

        public IEnumerable<Donor> StreamAllDonors()
        {
            return donorReadRepository.StreamAllDonors().Select(d => d.ToPublicDonor());
        }

        public async Task<IReadOnlyDictionary<int, Donor>> GetDonors(IEnumerable<int> donorIds)
        {
            var donorsByIds = await donorReadRepository.GetDonorsByIds(donorIds.ToList());
            return donorsByIds.ToDictionary(d => d.Key, d => d.Value.ToPublicDonor());
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<string>> GetExistingExternalDonorCodes(IEnumerable<string> externalDonorCodes)
        {
            return await donorReadRepository.GetExistingExternalDonorCodes(externalDonorCodes);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, int>> GetDonorIdsUpdatedSince(DateTimeOffset cutoffDate)
        {
            return await donorReadRepository.GetDonorIdsUpdatedSince(cutoffDate);
        }
    }
}