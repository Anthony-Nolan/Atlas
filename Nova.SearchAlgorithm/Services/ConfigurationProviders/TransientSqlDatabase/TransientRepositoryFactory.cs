using System;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase
{
    public interface ITransientRepositoryFactory
    {
        IDonorInspectionRepository GetDonorInspectionRepository(bool isActive = true);
        IDonorSearchRepository GetDonorSearchRepository(bool isActive = true);
        IDonorImportRepository GetDonorImportRepository(bool isActive = false);
        IDonorUpdateRepository GetDonorUpdateRepository(bool isActive = true);
        IDataRefreshRepository GetDataRefreshRepository(bool isActive = false);
        IPGroupRepository GetPGroupRepository(bool isActive = true);
    }
    
    public class TransientRepositoryFactory : ITransientRepositoryFactory
    {
        private readonly IConnectionStringProvider activeConnectionStringProvider;
        private readonly IConnectionStringProvider dormantConnectionStringProvider;

        public TransientRepositoryFactory(
            // ReSharper disable once SuggestBaseTypeForParameter
            ActiveTransientSqlConnectionStringProvider activeConnectionStringProvider, 
            // ReSharper disable once SuggestBaseTypeForParameter
            DormantTransientSqlConnectionStringProvider dormantConnectionStringProvider)
        {
            this.activeConnectionStringProvider = activeConnectionStringProvider;
            this.dormantConnectionStringProvider = dormantConnectionStringProvider;
        }

        public IDonorInspectionRepository GetDonorInspectionRepository(bool isActive)
        {
            return new DonorInspectionRepository(GetConnectionStringProvider(isActive));
        }

        public IDonorSearchRepository GetDonorSearchRepository(bool isActive)
        {
            return new DonorSearchRepository(GetConnectionStringProvider(isActive));
        }

        public IDonorImportRepository GetDonorImportRepository(bool isActive)
        {
            return new DonorImportRepository(GetPGroupRepository(isActive), GetConnectionStringProvider(isActive));
        }

        public IDonorUpdateRepository GetDonorUpdateRepository(bool isActive)
        {
            return new DonorUpdateRepository(GetPGroupRepository(isActive), GetConnectionStringProvider(isActive));
        }

        public IDataRefreshRepository GetDataRefreshRepository(bool isActive)
        {
            return new DataRefreshRepository(GetConnectionStringProvider(isActive));
        }

        public IPGroupRepository GetPGroupRepository(bool isActive)
        {
            return new PGroupRepository(GetConnectionStringProvider(isActive));
        }

        private IConnectionStringProvider GetConnectionStringProvider(bool isActive)
        {
            return isActive ? activeConnectionStringProvider : dormantConnectionStringProvider;
        }
    }
}