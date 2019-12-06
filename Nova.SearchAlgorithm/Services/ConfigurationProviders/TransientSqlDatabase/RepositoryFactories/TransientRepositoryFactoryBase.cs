using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Services;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface ITransientRepositoryFactory
    {
        IPGroupRepository GetPGroupRepository();
        IDonorInspectionRepository GetDonorInspectionRepository();
        IDonorUpdateRepository GetDonorUpdateRepository();
    }
    
    public abstract class TransientRepositoryFactoryBase : ITransientRepositoryFactory
    {
        protected readonly IConnectionStringProvider ConnectionStringProvider;

        protected TransientRepositoryFactoryBase(IConnectionStringProvider connectionStringProvider)
        {
            this.ConnectionStringProvider = connectionStringProvider;
        }

        public IPGroupRepository GetPGroupRepository()
        {
            return new PGroupRepository(ConnectionStringProvider);
        }

        public IDonorInspectionRepository GetDonorInspectionRepository()
        {
            return new DonorInspectionRepository(ConnectionStringProvider);
        }

        public IDonorUpdateRepository GetDonorUpdateRepository()
        {
            return new DonorUpdateRepository(GetPGroupRepository(), ConnectionStringProvider);
        }
    }
}