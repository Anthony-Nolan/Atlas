using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.DependencyInjection;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    /// <summary>
    /// These tests exist to test a specific interaction with service bus clients and transaction scope.
    /// Service bus clients do not support distributed transactions, so throw when called within one.
    /// Our workaround is to wrap the client itself in a suppressed transaction scope.
    ///
    /// As our other import tests involve mocking the service bus clients at a higher level, this suite purposefully doesn't mock them at the same level,
    /// and relies on custom behaviour of the mocked client to throw if a transaction is present.
    /// </summary>
    [TestFixture]
    public class TransactionScopeErrorTests
    {
        private IDonorFileImporter donorFileImporter;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [Test]
        public async Task ImportDonors_ForEdits_DoesNotThrow()
        {
            var donorEdit = DonorUpdateBuilder.New.Build(2).ToArray();

            var donorEditFile = DonorImportFileBuilder.NewWithoutContents.WithDonors(donorEdit);

            await donorFileImporter.Invoking(i => i.ImportDonorFile(donorEditFile)).Should().NotThrowAsync();
        }

        [Test]
        public async Task ImportDonors_WhenLastDonorInFileFails_DoesNotSendNotificationForEarlierDonor()
        {
            var mockMessagePublisher = Substitute.For<IMessageBatchPublisher<SearchableDonorUpdate>>();

            var services = ServiceConfiguration.BuildServiceCollection();
            services.AddScoped(sp => mockMessagePublisher);
            DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();

            var donorUpdate = DonorUpdateBuilder.New.Build();
            var donorEditFile = DonorImportFileBuilder.NewWithoutContents.WithDonors(donorUpdate, donorUpdate);

            try
            {
                await donorFileImporter.ImportDonorFile(donorEditFile);
            }
            catch (SqlException)
            {
                
            }
            finally
            {
                await mockMessagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
            }
        }
    }
}