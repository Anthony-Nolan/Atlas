using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    [NonParallelizable]
    public class DonorImportRepositoryTests
    {
        private IDonorImportRepository donorImportRepository;
        private string dormantConnectionString;
        private string activeConnectionString;

        [SetUp]
        public void SetUp()
        {
            DatabaseManager.ClearTransientDatabases();

            donorImportRepository = Injection.Provider.GetService<IDormantRepositoryFactory>().GetDonorImportRepository();
            dormantConnectionString = Injection.Provider.GetService<DormantTransientSqlConnectionStringProvider>().GetConnectionString();
            activeConnectionString = Injection.Provider.GetService<ActiveTransientSqlConnectionStringProvider>().GetConnectionString();
        }

        [Test]
        public async Task RemoveAllDonorInformation_TruncatesSubjectGenotypeSetTables()
        {
            await InsertSubjectGenotypeSetRows(dormantConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<SubjectGenotypeSetValue>(dormantConnectionString)).Should().Be(0);
            (await CountRows<DonorSubjectGenotypeSet>(dormantConnectionString)).Should().Be(0);
        }

        [Test]
        public async Task RemoveAllDonorInformation_DoesNotTruncateSubjectGenotypeSetTablesOnActiveDatabase()
        {
            await InsertSubjectGenotypeSetRows(activeConnectionString);

            await donorImportRepository.RemoveAllDonorInformation();

            (await CountRows<SubjectGenotypeSetValue>(activeConnectionString)).Should().Be(1);
            (await CountRows<DonorSubjectGenotypeSet>(activeConnectionString)).Should().Be(1);
        }

        private static async Task InsertSubjectGenotypeSetRows(string connectionString)
        {
            await using var context = new ContextFactory().Create(connectionString);

            var value = new SubjectGenotypeSetValue
            {
                HlaTypingKey = "typing-key",
                HaplotypeFrequencySetId = 1,
                AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1,
                IsUnrepresented = false,
                SubjectGenotypeSetData = new byte[] { 1, 2, 3 }
            };
            context.SubjectGenotypeSetValues.Add(value);
            await context.SaveChangesAsync();

            context.DonorSubjectGenotypeSets.Add(new DonorSubjectGenotypeSet
            {
                DonorId = 1,
                AllowedLociKey = AllowedLociKey.ABCDrb1Dqb1,
                SubjectGenotypeSetValueId = value.Id
            });
            await context.SaveChangesAsync();
        }

        private static async Task<int> CountRows<T>(string connectionString) where T : class
        {
            await using var context = new ContextFactory().Create(connectionString);
            return await context.Set<T>().CountAsync();
        }
    }
}
