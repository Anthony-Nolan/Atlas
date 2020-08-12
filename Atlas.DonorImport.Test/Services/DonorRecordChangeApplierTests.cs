using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Test.Services
{
    // TODO: ATLAS-427: Write unit (and integration) Tests for partial file updates.
    [TestFixture]
    internal class DonorRecordChangeApplierTests
    {
        private IMessagingServiceBusClient messagingServiceBusClient;
        private IDonorImportRepository donorImportRepository;
        private IDonorReadRepository donorInspectionRepository;
        private IDonorImportLogRepository donorImportLogRepository;

        private IDonorRecordChangeApplier donorOperationApplier;
        private IImportedLocusInterpreter naiveDnaLocusInterpreter;

        private readonly DonorImportFile defaultFile = new DonorImportFile {FileLocation = "file", UploadTime = DateTime.Now};

        [SetUp]
        public void SetUp()
        {
            messagingServiceBusClient = Substitute.For<IMessagingServiceBusClient>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            donorInspectionRepository = Substitute.For<IDonorReadRepository>();
            donorImportLogRepository = Substitute.For<IDonorImportLogRepository>();
            naiveDnaLocusInterpreter = Substitute.For<IImportedLocusInterpreter>();
            naiveDnaLocusInterpreter.Interpret(default, default).ReturnsForAnyArgs((call) =>
            {
                var arg = call.Arg<ImportedLocus>();
                return new LocusInfo<string>(arg?.Dna?.Field1, arg?.Dna?.Field2);
            });

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>());

            donorOperationApplier = new DonorRecordChangeApplier(messagingServiceBusClient, donorImportRepository, donorInspectionRepository, naiveDnaLocusInterpreter, donorImportLogRepository, Substitute.For<ILogger>());
        }

        [Test]
        public async Task ApplyDonorOperationBatch_WithCreationsOnly_WritesBatchToDatabase()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, int>
            {
                {donorUpdates[0].RecordId, 1},
                {donorUpdates[1].RecordId, 2},
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);

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

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(null)
                .ReturnsForAnyArgs(donorUpdates.ToDictionary(d => d.RecordId, d => 0));

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);

            await messagingServiceBusClient.Received(donorUpdates.Count).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithAMixOfOperations_PostsMatchingUpdatesForEachDonor()
        {
            //ARRANGE
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .With(d => d.ChangeType, new[] {ImportDonorChangeType.Create, ImportDonorChangeType.Delete, ImportDonorChangeType.Edit})
                .Build(21)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(default)
                .ReturnsForAnyArgs(args => args.Arg<ICollection<string>>().ToDictionary(code => code, code => 0));

            donorInspectionRepository.GetDonorsByExternalDonorCodes(default)
                .ReturnsForAnyArgs(args => args.Arg<ICollection<string>>().ToDictionary(code => code, code => new Donor {AtlasId = 0}));

            // Capture all the Calls to MessageServiceBus.
            var sequenceOfMassCalls = new List<List<SearchableDonorUpdate>>();
            var sequenceOfIndividualCalls = new List<SearchableDonorUpdate>();

            messagingServiceBusClient.PublishDonorUpdateMessages(
                Arg.Do<ICollection<SearchableDonorUpdate>>(messageCollection => { sequenceOfMassCalls.Add(messageCollection.ToList()); })
            ).Returns(Task.CompletedTask);

            messagingServiceBusClient.PublishDonorUpdateMessage(
                Arg.Do<SearchableDonorUpdate>(messageCollection => { sequenceOfIndividualCalls.Add(messageCollection); })
            ).Returns(Task.CompletedTask);
            

            //ACT
            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);
            

            //ASSERT
            sequenceOfMassCalls.Add(sequenceOfIndividualCalls);
            
            //Should be batched up in 3 sets of 7. Each of which maps to one ChangeType. IsAvailableForSearch is the closest surrogate we have to ChangeType.
            foreach (var massCall in sequenceOfMassCalls)
            {
                massCall.Select(call => call.IsAvailableForSearch).Should().AllBeEquivalentTo(massCall.First().IsAvailableForSearch);
                massCall.Should().HaveCount(7);
            }
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_IncludesNewlyAssignedAtlasIdInMatchingUpdate()
        {
            const int atlasId = 66;
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(null).ReturnsForAnyArgs(
                donorUpdates.ToDictionary(d => d.RecordId, d => atlasId)
            );

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);

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

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);

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

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile);

            await messagingServiceBusClient.DidNotReceive().PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }
    }
}