using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, string fileName);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IMessagingServiceBusClient messagingServiceBusClient;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorReadRepository donorInspectionRepository;
        private readonly IImportedLocusInterpreter locusInterpreter;
        private readonly ILogger logger;

        public DonorRecordChangeApplier(
            IMessagingServiceBusClient messagingServiceBusClient,
            IDonorImportRepository donorImportRepository,
            IDonorReadRepository donorInspectionRepository,
            IImportedLocusInterpreter locusInterpreter,
            ILogger logger)
        {
            this.donorImportRepository = donorImportRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
            this.donorInspectionRepository = donorInspectionRepository;
            this.locusInterpreter = locusInterpreter;
            this.logger = logger;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, string fileName)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        var creations = updatesOfSameOperationType.Select(update => MapToDatabaseDonor(update, fileName)).ToList();
                        await donorImportRepository.InsertDonorBatch(creations);
                        break;
                    case ImportDonorChangeType.Delete:
                        throw new NotImplementedException();
                    case ImportDonorChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var shouldFetchAtlasDonorIds = updatesOfSameOperationType.Any(u => u.UpdateMode != UpdateMode.Full);
                var atlasDonors = shouldFetchAtlasDonorIds
                    ? await donorInspectionRepository.GetDonorsByExternalDonorCodes(updatesOfSameOperationType.Select(u => u.RecordId))
                    : new Dictionary<string, Donor>();

                // The process of publishing and consuming update messages for the matching algorithm is very slow. 
                // For an initial load, donors will be imported in bulk to the matching algorithm, via a manually triggered process 
                foreach (var update in updatesOfSameOperationType)
                {
                    if (update.UpdateMode != UpdateMode.Full)
                    {
                        var atlasDonor = atlasDonors[update.RecordId];
                        if (atlasDonor == null)
                        {
                            throw new Exception($"Could not fnd created/updated donor in Atlas database: {update.RecordId}");
                        }

                        var atlasId = atlasDonor.AtlasId;
                        await messagingServiceBusClient.PublishDonorUpdateMessage(MapToMatchingUpdateMessage(update, atlasId));
                    }
                }
            }
        }

        private Donor MapToDatabaseDonor(DonorUpdate fileUpdate, string fileName)
        {
            locusInterpreter.SetDonorContext(fileUpdate, fileName);

            var interpretedA = locusInterpreter.Interpret(fileUpdate.Hla.A, Locus.A);
            var interpretedB = locusInterpreter.Interpret(fileUpdate.Hla.B, Locus.B);
            var interpretedC = locusInterpreter.Interpret(fileUpdate.Hla.C, Locus.C);
            var interpretedDpb1 = locusInterpreter.Interpret(fileUpdate.Hla.DPB1, Locus.Dpb1);
            var interpretedDqb1 = locusInterpreter.Interpret(fileUpdate.Hla.DQB1, Locus.Dqb1);
            var interpretedDrb1 = locusInterpreter.Interpret(fileUpdate.Hla.DRB1, Locus.Drb1);
            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode,
                A_1 = interpretedA.Position1,
                A_2 = interpretedA.Position2,
                B_1 = interpretedB.Position1,
                B_2 = interpretedB.Position2,
                C_1 = interpretedC.Position1,
                C_2 = interpretedC.Position2,
                DPB1_1 = interpretedDpb1.Position1,
                DPB1_2 = interpretedDpb1.Position2,
                DQB1_1 = interpretedDqb1.Position1,
                DQB1_2 = interpretedDqb1.Position2,
                DRB1_1 = interpretedDrb1.Position1,
                DRB1_2 = interpretedDrb1.Position2,
            };
            donor.Hash = donor.CalculateHash();
            return donor;
        }

        private SearchableDonorUpdate MapToMatchingUpdateMessage(DonorUpdate fileUpdate, int atlasId)
        {
            //TODO: Pass in a pre-interpreted DB Donor instead
            locusInterpreter.SetDonorContext(fileUpdate, "file");

            var interpretedA = locusInterpreter.Interpret(fileUpdate.Hla.A, Locus.A);
            var interpretedB = locusInterpreter.Interpret(fileUpdate.Hla.B, Locus.B);
            var interpretedC = locusInterpreter.Interpret(fileUpdate.Hla.C, Locus.C);
            var interpretedDpb1 = locusInterpreter.Interpret(fileUpdate.Hla.DPB1, Locus.Dpb1);
            var interpretedDqb1 = locusInterpreter.Interpret(fileUpdate.Hla.DQB1, Locus.Dqb1);
            var interpretedDrb1 = locusInterpreter.Interpret(fileUpdate.Hla.DRB1, Locus.Drb1);

            return new SearchableDonorUpdate
            {
                AuditId = 0,
                DonorId = atlasId,
                PublishedDateTime = DateTimeOffset.UtcNow,
                IsAvailableForSearch = fileUpdate.ChangeType != ImportDonorChangeType.Delete,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = atlasId,
                    DonorType = fileUpdate.DonorType.ToMatchingAlgorithmType(),
                    A_1 = interpretedA.Position1,
                    A_2 = interpretedA.Position2,
                    B_1 = interpretedB.Position1,
                    B_2 = interpretedB.Position2,
                    C_1 = interpretedC.Position1,
                    C_2 = interpretedC.Position2,
                    DPB1_1 = interpretedDpb1.Position1,
                    DPB1_2 = interpretedDpb1.Position2,
                    DQB1_1 = interpretedDqb1.Position1,
                    DQB1_2 = interpretedDqb1.Position2,
                    DRB1_1 = interpretedDrb1.Position1,
                    DRB1_2 = interpretedDrb1.Position2,
                }
            };
        }
    }
}