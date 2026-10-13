using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.Search.Precompute;

/// <summary>
/// The genotype set repository of a chosen transient database, for the match prediction hosts.
/// </summary>
/// <remarks>
/// Not <see cref="ConfigurationProviders.TransientSqlDatabase.RepositoryFactories.IStaticallyChosenDatabaseRepositoryFactory"/>:
/// that one needs the import logger and its logging context, which the match prediction hosts do not register.
/// </remarks>
public interface ISubjectGenotypeSetRepositoryFactory
{
    ISubjectGenotypeSetRepository GetForDatabase(TransientDatabase database);
}

internal class SubjectGenotypeSetRepositoryFactory : ISubjectGenotypeSetRepositoryFactory
{
    private readonly StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringProviderFactory;

    public SubjectGenotypeSetRepositoryFactory(StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringProviderFactory)
    {
        this.connectionStringProviderFactory = connectionStringProviderFactory;
    }

    public ISubjectGenotypeSetRepository GetForDatabase(TransientDatabase database) =>
        new SubjectGenotypeSetRepository(connectionStringProviderFactory.GenerateConnectionStringProvider(database));
}
