using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImportFailureService
    {
        Task SaveFailures(IReadOnlyCollection<DonorImportFailure> donorImportFailures);
    }

    internal class DonorImportFailureService : IDonorImportFailureService
    {
        private readonly IDonorImportFailureRepository failuresRepository;

        public DonorImportFailureService(IDonorImportFailureRepository failuresRepository)
        {
            this.failuresRepository = failuresRepository;
        }

        public async Task SaveFailures(IReadOnlyCollection<DonorImportFailure> donorImportFailures)
        {
            if (donorImportFailures.IsNullOrEmpty())
            {
                return;
            }

            await failuresRepository.BulkInsert(donorImportFailures);
        }
    }
}
