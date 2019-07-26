using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Performs all pre-processing required for running of the search algorithm:
        /// - Scales up target database 
        /// - Recreates Matching Dictionary
        /// - Imports all donors
        /// - Processes HLA for imported donors
        /// - Scales down target database
        /// </summary>
        Task RefreshData(string wmdaDatabaseVersion);
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly IOptions<DataRefreshSettings> settingsOptions;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IAzureDatabaseManager azureDatabaseManager;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettingsOptions,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IAzureDatabaseManager azureDatabaseManager,
            IDonorImportRepository donorImportRepository,
            IRecreateHlaLookupResultsService recreateMatchingDictionaryService,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.azureDatabaseManager = azureDatabaseManager;
            this.donorImportRepository = donorImportRepository;
            this.recreateMatchingDictionaryService = recreateMatchingDictionaryService;
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            settingsOptions = dataRefreshSettingsOptions;
        }

        public async Task RefreshData(string wmdaDatabaseVersion)
        {
            try
            {
                await ScaleUpDatabase();
                await donorImportRepository.RemoveAllDonorInformation();
                await recreateMatchingDictionaryService.RecreateAllHlaLookupResults(wmdaDatabaseVersion);
                await donorImporter.ImportDonors();
                await hlaProcessor.UpdateDonorHla(wmdaDatabaseVersion);
                await ScaleDownDatabase(settingsOptions.Value.ActiveDatabaseSize.ToAzureDatabaseSize());
            }
            catch (Exception)
            {
                await ScaleDownDatabase(settingsOptions.Value.DormantDatabaseSize.ToAzureDatabaseSize());
                throw;
            }
        }

        private async Task ScaleUpDatabase()
        {
            var settings = settingsOptions.Value;
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, settings.RefreshDatabaseSize.ToAzureDatabaseSize());
        }

        private async Task ScaleDownDatabase(AzureDatabaseSize targetSize)
        {
            var databaseName = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, targetSize);
        }
    }
}