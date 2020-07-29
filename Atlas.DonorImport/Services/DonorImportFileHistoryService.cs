using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;

namespace Atlas.DonorImport.Services
{

    internal interface IDonorImportFileHistoryService
    {
        public Task RegisterStartOfDonorImport(DonorImportFile donorFile);
        public Task RegisterSuccessfulDonorImport(DonorImportFile donorFile);
        public Task RegisterFailedDonorImportWithPermanentError(DonorImportFile donorFile);
        public Task RegisterUnexpectedDonorImportError(DonorImportFile donorFile);
    }
    
    internal class DonorImportFileHistoryService : IDonorImportFileHistoryService
    {
        private IDonorImportHistoryRepository repository;

        public DonorImportFileHistoryService(IDonorImportHistoryRepository repository)
        {
            this.repository = repository;
        }

        public async Task RegisterStartOfDonorImport(DonorImportFile donorFile)
        {
            var filename = GetFileNameFromLocation(donorFile.FileLocation);
            await repository.InsertNewDonorImport(filename, donorFile.UploadTime);
        }

        public async Task RegisterSuccessfulDonorImport(DonorImportFile donorFile)
        {
            await UpdateDonorImport(donorFile, DonorImportState.Completed);
        }

        public async Task RegisterFailedDonorImportWithPermanentError(DonorImportFile donorFile)
        {
            await UpdateDonorImport(donorFile, DonorImportState.FailedPermanent);
        }

        public async Task RegisterUnexpectedDonorImportError(DonorImportFile donorFile)
        {
            await UpdateDonorImport(donorFile, DonorImportState.FailedUnexpectedly);
        }

        private async Task UpdateDonorImport(DonorImportFile donorFile, DonorImportState state)
        {
            var filename = GetFileNameFromLocation(donorFile.FileLocation);
            await repository.UpdateDonorImportState(filename, donorFile.UploadTime, state);
        }
        
        private static string GetFileNameFromLocation(string location)
        {
            var i = location.LastIndexOf('/');
            return location.Substring(i + 1);
        }
    }
}