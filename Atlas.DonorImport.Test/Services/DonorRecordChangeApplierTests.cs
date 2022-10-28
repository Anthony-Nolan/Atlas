using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Notifications;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorUpdates;
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
        private IDonorImportRepository donorImportRepository;
        private IDonorReadRepository donorInspectionRepository;
        private IDonorImportLogService donorImportLogService;
        private IDonorUpdatesSaver updatesSaver;
        private IDonorImportFileHistoryService donorImportHistoryService;
        private INotificationSender notificationSender;

        private IDonorRecordChangeApplier donorOperationApplier;
        private IImportedLocusInterpreter naiveDnaLocusInterpreter;

        private readonly DonorImportFile defaultFile = new DonorImportFile { FileLocation = "file", UploadTime = DateTime.Now };

        [SetUp]
        public void SetUp()
        {
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            donorInspectionRepository = Substitute.For<IDonorReadRepository>();
            updatesSaver = Substitute.For<IDonorUpdatesSaver>();
            donorImportLogService = Substitute.For<IDonorImportLogService>();
            donorImportHistoryService = Substitute.For<IDonorImportFileHistoryService>();
            notificationSender = Substitute.For<INotificationSender>();
            naiveDnaLocusInterpreter = Substitute.For<IImportedLocusInterpreter>();
            naiveDnaLocusInterpreter.Interpret(default, default).ReturnsForAnyArgs((call) =>
            {
                var arg = call.Arg<ImportedLocus>();
                return new LocusInfo<string>(arg?.Dna?.Field1, arg?.Dna?.Field2);
            });

            donorInspectionRepository.GetDonorsByExternalDonorCodes(null).ReturnsForAnyArgs(new Dictionary<string, Donor>());

            donorOperationApplier = new DonorRecordChangeApplier(
                donorImportRepository,
                donorInspectionRepository,
                naiveDnaLocusInterpreter,
                updatesSaver,
                donorImportLogService,
                donorImportHistoryService,
                notificationSender,
                new NotificationConfigurationSettings { NotifyOnAttemptedDeletionOfUntrackedDonor = true }
            );
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
                { donorUpdates[0].RecordId, 1 },
                { donorUpdates[1].RecordId, 2 },
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            await donorImportRepository.Received().InsertDonorBatch(Arg.Is<IEnumerable<Donor>>(storedDonors =>
                storedDonors.Count() == donorUpdates.Count)
            );
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithCreationsOnly_SavesAPublishableUpdateForEachDonor()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(null)
                .ReturnsForAnyArgs(donorUpdates.ToDictionary(d => d.RecordId, d => 0));

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            await updatesSaver
                .Received(1)
                .Save(Arg.Is<List<SearchableDonorUpdate>>(messages => messages.Count == donorUpdates.Count));
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithAMixOfOperations_SavesPublishableUpdateForEachDonor()
        {
            //ARRANGE
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .With(d => d.ChangeType, new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Delete, ImportDonorChangeType.Edit })
                .Build(21)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(default)
                .ReturnsForAnyArgs(args => args.Arg<ICollection<string>>().ToDictionary(code => code, code => 0));

            donorInspectionRepository.GetDonorsByExternalDonorCodes(default)
                .ReturnsForAnyArgs(args => args.Arg<ICollection<string>>().ToDictionary(code => code, code => new Donor { AtlasId = 0 }));

            // Capture all the saved messages.
            var sequenceOfMassCalls = new List<List<SearchableDonorUpdate>>();

            updatesSaver.Save(
                Arg.Do<IReadOnlyCollection<SearchableDonorUpdate>>(publishableDonorUpdates => { sequenceOfMassCalls.Add(publishableDonorUpdates.ToList()); })
            ).Returns(Task.CompletedTask);

            //ACT
            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            //ASSERT
            //Should be batched up in 3 sets of 7. Each of which maps to one ChangeType. IsAvailableForSearch is the closest surrogate we have to ChangeType.
            foreach (var massCall in sequenceOfMassCalls)
            {
                massCall.Select(call => call.IsAvailableForSearch).Should().AllBeEquivalentTo(massCall.First().IsAvailableForSearch);
                massCall.Should().HaveCount(7);
            }
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_IncludesNewlyAssignedAtlasIdInPublishableDonorUpdate()
        {
            const int atlasId = 66;
            var donorUpdates = new List<DonorUpdate>
            {
                DonorUpdateBuilder.New.With(d => d.UpdateMode, UpdateMode.Differential).Build(),
            };

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(null).ReturnsForAnyArgs(
                donorUpdates.ToDictionary(d => d.RecordId, d => atlasId)
            );

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            await updatesSaver.Received(1).Save(Arg.Is<List<SearchableDonorUpdate>>(messages =>
                messages.All(u => u.DonorId == atlasId)
            ));
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForFullUpdate_DoesNotSavePublishableDonorUpdates()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Full)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(default).ReturnsForAnyArgs(new Dictionary<string, int>());

            donorInspectionRepository.GetDonorsByExternalDonorCodes(default).ReturnsForAnyArgs(new Dictionary<string, Donor>
            {
                { donorUpdates[0].RecordId, new Donor { AtlasId = 1, ExternalDonorCode = donorUpdates[0].RegistryCode } },
                { donorUpdates[1].RecordId, new Donor { AtlasId = 2, ExternalDonorCode = donorUpdates[1].RegistryCode } },
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            await updatesSaver.DidNotReceiveWithAnyArgs().Save(default);
        }

        [Test]
        public async Task ApplyDonorOperationBatch_ForDifferentialUpdate_WithDeletesForDonorsThatDoNotExist_SendsNotification()
        {
            var donorUpdates = DonorUpdateBuilder.New
                .With(d => d.UpdateMode, UpdateMode.Differential)
                .With(d => d.ChangeType, ImportDonorChangeType.Delete)
                .Build(2)
                .ToList();

            donorInspectionRepository.GetDonorIdsByExternalDonorCodes(default).ReturnsForAnyArgs(new Dictionary<string, int>());

            donorInspectionRepository.GetDonorsByExternalDonorCodes(default).ReturnsForAnyArgs(new Dictionary<string, Donor>
            {
                { donorUpdates[0].RecordId, new Donor { AtlasId = 1, ExternalDonorCode = donorUpdates[0].RegistryCode } },
            });

            await donorOperationApplier.ApplyDonorRecordChangeBatch(donorUpdates, defaultFile, 0);

            await notificationSender.Received().SendNotification(
                Arg.Any<string>(),
                Arg.Is<string>(m => donorUpdates.All(d => m.Contains(d.RecordId))),
                Arg.Any<string>());
        }
    }
}