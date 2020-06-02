using System;
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
        private IDonorImportRepository donorImportRepository;
        private IMessagingServiceBusClient messagingServiceBusClient;

        private IDonorRecordChangeApplier donorOperationApplier;

        [SetUp]
        public void SetUp()
        {
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            messagingServiceBusClient = Substitute.For<IMessagingServiceBusClient>();

            donorRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>());

            donorOperationApplier = new DonorRecordChangeApplier(donorImportRepository, messagingServiceBusClient);
        }

        [Test]
        public async Task ApplyDonorOperationBatch_WithCreationsOnly_WritesBatchToDatabase()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-1").With(d => d.UpdateMode, UpdateMode.Differential).Build(),
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-2").With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            donorRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>
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
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-1").With(d => d.UpdateMode, UpdateMode.Differential).Build(),
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-2").With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            donorRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(donorUpdates.ToDictionary(d => d.RecordId, d => new Donor()));

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

            donorRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(
                donorUpdates.ToDictionary(d => d.RecordId, d => new Donor {AtlasId = atlasId})
            );

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.Received(donorUpdates.Count).PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == atlasId
            ));
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForFullUpdate_DoesNotPostMatchingUpdates()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-1").With(d => d.UpdateMode, UpdateMode.Full).Build(),
                DonorUpdateBuilder.New.With(d => d.RecordId, "donor-2").With(d => d.UpdateMode, UpdateMode.Full).Build(),
            };

            donorRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>
            {
                {donorUpdates[0].RecordId, new Donor {AtlasId = 1, ExternalDonorCode = donorUpdates[0].RegistryCode}},
                {donorUpdates[1].RecordId, new Donor {AtlasId = 2, ExternalDonorCode = donorUpdates[1].RegistryCode}},
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.DidNotReceive().PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }
    }
}