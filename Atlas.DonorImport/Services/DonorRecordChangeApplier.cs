using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
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
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IMessagingServiceBusClient messagingServiceBusClient;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorReadRepository donorInspectionRepository;
        private readonly IHlaCategorisationService hlaCategoriser;
        private readonly ILogger logger;

        public DonorRecordChangeApplier(
            IMessagingServiceBusClient messagingServiceBusClient,
            IDonorImportRepository donorImportRepository,
            IDonorReadRepository donorInspectionRepository,
            IHlaCategorisationService hlaCategoriser,
            ILogger logger)
        {
            this.donorImportRepository = donorImportRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
            this.donorInspectionRepository = donorInspectionRepository;
            this.hlaCategoriser = hlaCategoriser;
            this.logger = logger;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        await donorImportRepository.InsertDonorBatch(updatesOfSameOperationType.Select(MapToDatabaseDonor));
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

        private Donor MapToDatabaseDonor(DonorUpdate fileUpdate)
        {
            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode,
                A_1 = fileUpdate.Hla.A.ReadField1(hlaCategoriser, logger),
                A_2 = fileUpdate.Hla.A.ReadField2(hlaCategoriser, logger),
                B_1 = fileUpdate.Hla.B.ReadField1(hlaCategoriser, logger),
                B_2 = fileUpdate.Hla.B.ReadField2(hlaCategoriser, logger),
                C_1 = fileUpdate.Hla.C?.ReadField1(hlaCategoriser, logger),
                C_2 = fileUpdate.Hla.C?.ReadField2(hlaCategoriser, logger),
                DPB1_1 = fileUpdate.Hla.DPB1?.ReadField1(hlaCategoriser, logger),
                DPB1_2 = fileUpdate.Hla.DPB1?.ReadField2(hlaCategoriser, logger),
                DQB1_1 = fileUpdate.Hla.DQB1?.ReadField1(hlaCategoriser, logger),
                DQB1_2 = fileUpdate.Hla.DQB1?.ReadField2(hlaCategoriser, logger),
                DRB1_1 = fileUpdate.Hla.DRB1.ReadField1(hlaCategoriser, logger),
                DRB1_2 = fileUpdate.Hla.DRB1.ReadField2(hlaCategoriser, logger),
            };
            donor.Hash = donor.CalculateHash();
            return donor;
        }

        private SearchableDonorUpdate MapToMatchingUpdateMessage(DonorUpdate fileUpdate, int atlasId)
        {
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
                    A_1 = fileUpdate.Hla.A.ReadField1(hlaCategoriser, logger),
                    A_2 = fileUpdate.Hla.A.ReadField2(hlaCategoriser, logger),
                    B_1 = fileUpdate.Hla.B.ReadField1(hlaCategoriser, logger),
                    B_2 = fileUpdate.Hla.B.ReadField2(hlaCategoriser, logger),
                    C_1 = fileUpdate.Hla.C.ReadField1(hlaCategoriser, logger),
                    C_2 = fileUpdate.Hla.C.ReadField2(hlaCategoriser, logger),
                    DPB1_1 = fileUpdate.Hla.DPB1.ReadField1(hlaCategoriser, logger),
                    DPB1_2 = fileUpdate.Hla.DPB1.ReadField2(hlaCategoriser, logger),
                    DQB1_1 = fileUpdate.Hla.DQB1.ReadField1(hlaCategoriser, logger),
                    DQB1_2 = fileUpdate.Hla.DQB1.ReadField2(hlaCategoriser, logger),
                    DRB1_1 = fileUpdate.Hla.DRB1.ReadField1(hlaCategoriser, logger),
                    DRB1_2 = fileUpdate.Hla.DRB1.ReadField2(hlaCategoriser, logger),
                }
            };
        }
    }
}