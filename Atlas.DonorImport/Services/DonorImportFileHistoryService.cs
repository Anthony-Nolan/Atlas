using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
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
            var state = await repository.GetFileStateIfExists(filename, donorFile.UploadTime);
            switch (state)
            {
                case DonorImportState.NotFound:
                    await repository.InsertNewDonorImportRecord(filename, donorFile.UploadTime);
                    break;
                case DonorImportState.FailedUnexpectedly:
                    await UpdateDonorImportRecord(donorFile, DonorImportState.Started);
                    break;
                default:
                    throw new DuplicateDonorImportException($"Duplicate Donor File Import Attempt. File: {donorFile.FileLocation} was started but already had an entry of state: {state}");
            }
            
        }

        public async Task RegisterSuccessfulDonorImport(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.Completed);
        }

        public async Task RegisterFailedDonorImportWithPermanentError(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.FailedPermanent);
        }

        public async Task RegisterUnexpectedDonorImportError(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.FailedUnexpectedly);
        }

        private async Task UpdateDonorImportRecord(DonorImportFile donorFile, DonorImportState state)
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