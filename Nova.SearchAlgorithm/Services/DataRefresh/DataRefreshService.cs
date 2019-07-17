using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.AzureManagement;
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
        Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion);
    }

    public class DataRefreshService : IDataRefreshService
    {
        private readonly DataRefreshSettings settings;
        private readonly IAzureFunctionManager azureFunctionManager;
        private readonly IAzureDatabaseManager azureDatabaseManager;

        private readonly IDonorImportRepository donorImportRepository;

        private readonly IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
        private readonly IDonorImporter donorImporter;
        private readonly IHlaProcessor hlaProcessor;

        public DataRefreshService(
            IOptions<DataRefreshSettings> dataRefreshSettings,
            IAzureFunctionManager azureFunctionManager,
            IAzureDatabaseManager azureDatabaseManager,
            IDonorImportRepository donorImportRepository,
            IRecreateHlaLookupResultsService recreateMatchingDictionaryService,
            IDonorImporter donorImporter,
            IHlaProcessor hlaProcessor)
        {
            this.azureFunctionManager = azureFunctionManager;
            this.azureDatabaseManager = azureDatabaseManager;
            this.donorImportRepository = donorImportRepository;
            this.recreateMatchingDictionaryService = recreateMatchingDictionaryService;
            this.donorImporter = donorImporter;
            this.hlaProcessor = hlaProcessor;
            settings = dataRefreshSettings.Value;
        }

        public async Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion)
        {
            await AzureInfrastructureSetUp(databaseToRefresh);
            await donorImportRepository.RemoveAllDonorInformation();

//            await recreateMatchingDictionaryService.RecreateAllHlaLookupResults(wmdaDatabaseVersion);
            await donorImporter.ImportDonors();
            await hlaProcessor.UpdateDonorHla(wmdaDatabaseVersion);

            await AzureInfrastructureTearDown(databaseToRefresh);
        }

        private async Task AzureInfrastructureSetUp(TransientDatabase refreshDatabase)
        {
            await Task.WhenAll(
                azureFunctionManager.StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName),
                azureDatabaseManager.UpdateDatabaseSize(GetAzureDatabaseName(refreshDatabase), settings.RefreshDatabaseSize.ToAzureDatabaseSize())
            );
        }

        private async Task AzureInfrastructureTearDown(TransientDatabase refreshDatabase)
        {
            await Task.WhenAll(
                azureDatabaseManager.UpdateDatabaseSize(GetAzureDatabaseName(refreshDatabase), settings.ActiveDatabaseSize.ToAzureDatabaseSize()),
                azureDatabaseManager.UpdateDatabaseSize(GetOtherDatabaseName(refreshDatabase), settings.DormantDatabaseSize.ToAzureDatabaseSize()),
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

        private string GetOtherDatabaseName(TransientDatabase databaseToRefresh)
        {
            switch (databaseToRefresh)
            {
                case TransientDatabase.DatabaseA:
                    return settings.DatabaseBName;
                case TransientDatabase.DatabaseB:
                    return settings.DatabaseAName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseToRefresh), databaseToRefresh, null);
            }
        }
    }
}