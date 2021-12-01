using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.DifferentialUpdates
{
    [TestFixture]
    public class DifferentialDonorUpsertTests
    {
        private IDonorInspectionRepository donorRepository;
        private IMessagingServiceBusClient mockServiceBusClient;
        private IDonorFileImporter fileImporter;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;

        private const string DonorCodePrefix = "external-donor-code-";
        
        private Builder<DonorUpdate> DonorBuilder => DonorUpdateBuilder.New
            .WithRecordIdPrefix(DonorCodePrefix)
            .With(du => du.ChangeType, ImportDonorChangeType.Upsert);

        private const int InitialDonorsCount = 5;
        private List<Donor> InitialDonors { get; set; }

        private const string Hla1 = "*01:01";
        private const string Hla2 = "*01:02";
        private const string Hla3 = "*01:03";
        private readonly ImportedHla hlaObject1 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(Hla1).Build();
        private readonly ImportedHla hlaObject2 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(Hla2).Build();
        private readonly ImportedHla hlaObject3 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(Hla3).Build();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockServiceBusClient = Substitute.For<IMessagingServiceBusClient>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddTransient(sp => mockServiceBusClient);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();

                mockServiceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<IMessagingServiceBusClient>();
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                fileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
            });
        }

        [SetUp]
        public async Task SetUp()
        {
            var initialDonors = DonorBuilder
                .With(du => du.ChangeType, ImportDonorChangeType.Create)
                .With(du => du.Hla, new[] {hlaObject1, hlaObject2})
                .Build(InitialDonorsCount).ToArray();

            var initialDonorsFile = fileBuilder.WithDonors(initialDonors).Build();

            await fileImporter.ImportDonorFile(initialDonorsFile);

            InitialDonors = donorRepository.StreamAllDonors().ToList();
            InitialDonors.Should().HaveCount(InitialDonorsCount);
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_ForUpsert_DonorsNotExist_CreatesDonors()
        {
            const int additionCount = 3;

            var additionDonors = DonorBuilder.Build(additionCount).ToArray();

            var donorsFile = fileBuilder.WithDonors(additionDonors);

            // ACT
            await fileImporter.ImportDonorFile(donorsFile);

            var donors = donorRepository.StreamAllDonors().ToList();
            donors.Should().HaveCount(InitialDonorsCount + additionCount);
        }

        [Test]
        public async Task ImportDonors_ForUpsert_DonorsExists_UpdateDonors()
        {
            const int modifiedCount = 3;

            var modifiedDonors = DonorBuilder
                .With(du => du.RecordId, InitialDonors.Select(donor => donor.ExternalDonorCode))
                .With(du => du.Hla, hlaObject3)
                .Build(modifiedCount).ToArray();
            var modifiedDonorCodes = modifiedDonors.Select(d => d.RecordId).ToArray();

            var donorsFile = fileBuilder.WithDonors(modifiedDonors);

            // ACT
            await fileImporter.ImportDonorFile(donorsFile);

            var donors = donorRepository.StreamAllDonors().ToList();

            donors.Should().HaveCount(InitialDonorsCount);

            foreach (var donor in donors)
            {
                if (modifiedDonorCodes.Contains(donor.ExternalDonorCode))
                {
                    donor.A_1.Should().Be(Hla3);
                }
                else
                {
                    donor.A_1.Should().NotBe(Hla3);
                }
            }
        }

        [Test]
        public async Task ImportDonors_ForUpsert_MixedExistAndNonExistDonors_UpdateIfExistsAndCreateIfNotExists()
        {
            const int additionCount = 2;
            const int modifiedCount = 3;

            var additionDonors = DonorBuilder
                .With(du => du.Hla, hlaObject1)
                .Build(additionCount).ToArray();

            var modifiedDonors = DonorBuilder
                .With(du => du.RecordId, InitialDonors.Select(d => d.ExternalDonorCode))
                .With(du => du.Hla, hlaObject3)
                .Build(modifiedCount).ToArray();
            var modifiedDonorCodes = modifiedDonors.Select(d => d.RecordId).ToArray();

            var mixedDonors = additionDonors.Union(modifiedDonors).ToArray();
            var mixedDonorsFile = fileBuilder.WithDonors(mixedDonors);

            // ACT
            await fileImporter.ImportDonorFile(mixedDonorsFile);

            var donors = donorRepository.StreamAllDonors().ToList();

            donors.Should().HaveCount(InitialDonorsCount + additionCount);

            foreach (var donor in donors)
            {
                if (modifiedDonorCodes.Contains(donor.ExternalDonorCode))
                {
                    donor.A_1.Should().Be(Hla3);
                }
                else
                {
                    donor.A_1.Should().NotBe(Hla3);
                }
            }
        }

        [Test]
        public async Task ImportDonors_ForUpsert_NoPertinentInfoChanged_DatabaseNotChangedNorSendMessages()
        {
            var modifiedDonor = DonorBuilder
                .With(du => du.RecordId, InitialDonors.Select(d => d.ExternalDonorCode))
                .With(du => du.Hla, hlaObject1)
                .Build();
            var donorFile = fileBuilder.WithDonors(modifiedDonor);

            mockServiceBusClient.ClearReceivedCalls();

            // ACT
            await fileImporter.ImportDonorFile(donorFile);

            var dbDonor = await donorRepository.GetDonor(modifiedDonor.RecordId);
            var initDonor = InitialDonors.Take(1).Single();

            initDonor.Should().BeEquivalentTo(dbDonor);
            await mockServiceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessages(default);
        }

        [Test]
        public async Task ImportDonors_ForUpsert_MixedExistAndNonExistDonors_CreatesOrUpdatesDonorsAndSendsMessagesMatchingTheNewPropertiesAndAtlasIds()
        {
            const int additionCount = 2;
            const int modifiedCount = 3;

            var additionDonors = DonorBuilder
                .With(du => du.Hla, hlaObject1)
                .Build(additionCount).ToArray();

            var modifiedDonors = DonorBuilder
                .With(du => du.RecordId, InitialDonors.Select(d => d.ExternalDonorCode))
                .With(du => du.Hla, hlaObject3)
                .Build(modifiedCount).ToArray();

            var mixedDonors = additionDonors.Union(modifiedDonors).ToArray();
            var mixedDonorsFile = fileBuilder.WithDonors(mixedDonors);

            mockServiceBusClient.ClearReceivedCalls();
            var capturedUpdates = ConfigureCapturingOfUpdateMessageBatches();

            // ACT
            await fileImporter.ImportDonorFile(mixedDonorsFile);

            foreach (var modifiedDonor in modifiedDonors)
            {
                var modifiedDbDonor = await donorRepository.GetDonor(modifiedDonor.RecordId);
                capturedUpdates.Should().ContainSingle(message =>
                    message.DonorId == modifiedDbDonor.AtlasId && message.SearchableDonorInformation.A_1 == Hla3);
            }

            foreach (var additionDonor in additionDonors)
            {
                var additionDbDonor = await donorRepository.GetDonor(additionDonor.RecordId);
                capturedUpdates.Should().ContainSingle(message =>
                    message.DonorId == additionDbDonor.AtlasId && message.SearchableDonorInformation.DonorId == additionDbDonor.AtlasId);
            }

            capturedUpdates.Should().HaveCount(additionCount + modifiedCount);
        }
        
        private List<SearchableDonorUpdate> ConfigureCapturingOfUpdateMessageBatches()
        {
            var capturedUpdates = new List<SearchableDonorUpdate>();
            mockServiceBusClient
                .When(client => client.PublishDonorUpdateMessages(Arg.Any<ICollection<SearchableDonorUpdate>>()))
                .Do(clientCallArgs => capturedUpdates.AddRange(clientCallArgs.Arg<ICollection<SearchableDonorUpdate>>()));
            mockServiceBusClient.ClearReceivedCalls();
            
            return capturedUpdates;
        }
    }
}
