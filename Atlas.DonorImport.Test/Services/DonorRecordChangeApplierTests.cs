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
        private IDonorRepository donorRepository;
        private IMessagingServiceBusClient messagingServiceBusClient;

        private IDonorRecordChangeApplier donorOperationApplier;

        [SetUp]
        public void SetUp()
        {
            donorRepository = Substitute.For<IDonorRepository>();
            messagingServiceBusClient = Substitute.For<IMessagingServiceBusClient>();

            donorOperationApplier = new DonorRecordChangeApplier(donorRepository, messagingServiceBusClient);
        }

        [Test]
        public async Task ApplyDonorOperationBatch_WithCreationsOnly_WritesBatchToDatabase()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.Build(),
                DonorUpdateBuilder.New.Build()
            };

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await donorRepository.Received().InsertDonorBatch(Arg.Is<IEnumerable<Donor>>(storedDonors =>
                storedDonors.Count() == donorUpdates.Count)
            );
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithCreationsOnly_PostsAMatchingUpdateForEachDonor()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Differential).Build(),
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.Received(donorUpdates.Count).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForInitialUpdate_DoesNotPostMatchingUpdates()
        {
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Full).Build(),
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Full).Build()
            };

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates);

            await messagingServiceBusClient.DidNotReceive().PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }
    }
}