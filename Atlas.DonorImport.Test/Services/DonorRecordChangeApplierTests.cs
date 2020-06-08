using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorRecordChangeApplierTests
    {
        private IMessagingServiceBusClient messagingServiceBusClient;
        private IDonorImportRepository donorImportRepository;
        private IDonorReadRepository donorInspectionRepository;

        private IDonorRecordChangeApplier donorOperationApplier;

        [SetUp]
        public void SetUp()
        {
            messagingServiceBusClient = Substitute.For<IMessagingServiceBusClient>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            donorInspectionRepository = Substitute.For<IDonorReadRepository>();

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>());

            donorOperationApplier = new DonorRecordChangeApplier(messagingServiceBusClient, donorImportRepository, donorInspectionRepository);
        }

        [Test]
        public async Task ApplyDonorOperationBatch_WithCreationsOnly_WritesBatchToDatabase()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>
            {
                {donorUpdates[0].RecordId, new Donor {AtlasId = 1, ExternalDonorCode = donorUpdates[0].RegistryCode}},
                {donorUpdates[1].RecordId, new Donor {AtlasId = 2, ExternalDonorCode = donorUpdates[1].RegistryCode}},
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await donorImportRepository.Received().InsertDonorBatch(Arg.Is<IEnumerable<Donor>>(storedDonors =>
                storedDonors.Count() == donorUpdates.Count)
            );
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithCreationsOnly_PostsAMatchingUpdateForEachDonor()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null)
                .ReturnsForAnyArgs(donorUpdates.ToDictionary(d => d.RecordId, d => new Donor()));

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.Received(donorUpdates.Count).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_IncludesNewlyAssignedAtlasIdInMatchingUpdate()
        {
            const int atlasId = 66;
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(
                donorUpdates.ToDictionary(d => d.RecordId, d => new Donor {AtlasId = atlasId})
            );

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.Received(donorUpdates.Count).PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == atlasId
            ));
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForFullUpdate_DoesNotFetchNewlyAssignedAtlasIds()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Full).Build(),
            };

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await donorInspectionRepository.DidNotReceiveWithAnyArgs().GetDonorsByExternalDonorCodes(null);
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForFullUpdate_DoesNotPostMatchingUpdates()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Full)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>
            {
                {donorUpdates[0].RecordId, new Donor {AtlasId = 1, ExternalDonorCode = donorUpdates[0].RegistryCode}},
                {donorUpdates[1].RecordId, new Donor {AtlasId = 2, ExternalDonorCode = donorUpdates[1].RegistryCode}},
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.DidNotReceive().PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }
    }
}