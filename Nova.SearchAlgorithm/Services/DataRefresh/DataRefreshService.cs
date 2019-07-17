using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Performs all pre-processing required for running of the search algorithm:
        /// - Recreates Matching Dictionary
        /// - Imports all donors
        /// - Processes HLA for imported donors
        /// Also coordinates infrastructure changes necessary for the performant running of the data refresh
        /// </summary>
        Task RefreshData(string wmdaDatabaseVersion);
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly DataRefreshSettings settings;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IAzureDatabaseManager azureDatabaseManager;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettings,
            IActiveDatabaseProvider activeDatabaseProvider,
            IAzureFunctionManager azureFunctionManager,
            IAzureDatabaseManager azureDatabaseManager,
            IDonorImportRepository donorImportRepository,
            IRecreateHlaLookupResultsService recreateMatchingDictionaryService,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.azureFunctionManager = azureFunctionManager;
            this.azureDatabaseManager = azureDatabaseManager;
            this.donorImportRepository = donorImportRepository;
            this.recreateMatchingDictionaryService = recreateMatchingDictionaryService;
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            settings = dataRefreshSettings.Value;
        }

        public async Task RefreshData(string wmdaDatabaseVersion)
        {
            await AzureInfrastructureSetUp();
            await donorImportRepository.RemoveAllDonorInformation();

            await recreateMatchingDictionaryService.RecreateAllHlaLookupResults(wmdaDatabaseVersion);
            await donorImporter.ImportDonors();
            await hlaProcessor.UpdateDonorHla(wmdaDatabaseVersion);

            await AzureInfrastructureTearDown();
        }

        private async Task AzureInfrastructureSetUp()
        {
            var databaseName = GetAzureDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            
            await Task.WhenAll(
                azureFunctionManager.StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName),
                azureDatabaseManager.UpdateDatabaseSize(databaseName, settings.RefreshDatabaseSize.ToAzureDatabaseSize())
            );
        }

        private async Task AzureInfrastructureTearDown()
        {
            var refreshDatabaseName = GetAzureDatabaseName(activeDatabaseProvider.GetDormantDatabase());
            var otherDatabaseName = GetAzureDatabaseName(activeDatabaseProvider.GetActiveDatabase());
            
            await Task.WhenAll(
                azureDatabaseManager.UpdateDatabaseSize(refreshDatabaseName, settings.ActiveDatabaseSize.ToAzureDatabaseSize()),
                azureDatabaseManager.UpdateDatabaseSize(otherDatabaseName, settings.DormantDatabaseSize.ToAzureDatabaseSize()),
                azureFunctionManager.StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName)
            );
        }

        private string GetAzureDatabaseName(TransientDatabase transientDatabaseType)
        {
            switch (transientDatabaseType)
            {
                case TransientDatabase.DatabaseA:
                    return settings.DatabaseAName;
                case TransientDatabase.DatabaseB:
                    return settings.DatabaseBName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transientDatabaseType), transientDatabaseType, null);
            }
        }
    }
}