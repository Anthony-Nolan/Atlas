using System;
using System.Linq;
using System.Threading.Tasks;
using Nova.DonorService.Client;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public class DonorImportService : IDonorImportService
    {
        // TODO:NOVA-1170 for now just import 10. Increase batch size later.
        private const int DonorPageSize = 10;

        private readonly IDonorInspectionRepository donorInspectionRespository;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly ILogger logger;

        public DonorImportService(
            IDonorInspectionRepository donorInspectionRespository,
            IDonorImportRepository donorImportRepository,
            IMatchingDictionaryLookupService lookupService,
            IDonorServiceClient donorServiceClient,
            ILogger logger)
        {
            this.donorInspectionRespository = donorInspectionRespository;
            this.donorImportRepository = donorImportRepository;
            this.lookupService = lookupService;
            this.donorServiceClient = donorServiceClient;
            this.logger = logger;
        }

        public async Task StartDonorImport()
        {
            try
            {
                await ContinueDonorImport(await donorInspectionRespository.HighestDonorId());
            }
            catch (Exception ex)
            {
                throw new DonorImportHttpException("Unable to complete donor import.", ex);
            }
        }

        public async Task ContinueDonorImport(int lastId)
        {
            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {lastId} onwards", LogLevel.Trace);

            var page = await donorServiceClient.GetDonors(DonorPageSize, lastId);

            if (page.Donors.Any())
            {
                // TODO:NOVA-1170: Insert in batches for efficiency
                // TODO:NOVA-1170: Log exceptions and continue to other donors
                foreach (var donor in page.Donors)
                {
                    await InsertRawDonor(donor);
                }

                var nextId = page.LastId ?? (await donorInspectionRespository.HighestDonorId());

                await ContinueDonorImport(nextId);
            }
            else
            {
                logger.SendTrace("Donor import complete", LogLevel.Info);
            }
        }

        private async Task InsertRawDonor(Donor donor)
        {
            await donorImportRepository.AddOrUpdateDonor(new InputDonor
            {
                RegistryCode = RegistryCodeFromString(donor.RegistryCode),
                DonorType = DonorTypeFromString(donor.DonorType),
                DonorId = donor.DonorId,
                MatchingHla = await LookupDonorHla(donor)
            });
        }
        private Task<PhenotypeInfo<ExpandedHla>> LookupDonorHla(Donor donor)
        {
            var donorHla = new PhenotypeInfo<string>
            {
                A_1 = donor.A_1,
                A_2 = donor.A_2,
                B_1 = donor.B_1,
                B_2 = donor.B_2,
                C_1 = donor.C_1,
                C_2 = donor.C_2,
                DQB1_1 = donor.DQB1_1,
                DQB1_2 = donor.DQB1_2,
                DRB1_1 = donor.DRB1_1,
                DRB1_2 = donor.DRB1_2
            };

            return donorHla.WhenAll(LookupMatchingHla);
        }

        private async Task<ExpandedHla> LookupMatchingHla(Locus locus, string hla)
        {
            if (string.IsNullOrEmpty(hla))
            {
                return null;
            }

            var matchingResult = await lookupService.GetMatchingHla(locus.ToMatchLocus(), hla);
            return matchingResult.ToExpandedHla();
        }

        private static RegistryCode RegistryCodeFromString(string input)
        {
            if (Enum.TryParse(input, out RegistryCode code))
            {
                return code;
            }
            throw new DonorImportException($"Could not understand registry code {input}");
        }

        private static DonorType DonorTypeFromString(string input)
        {
            switch (input.ToLower())
            {
                case "adult":
                case "a":
                    return DonorType.Adult;
                case "cord":
                case "c":
                    return DonorType.Cord;
                default:
                    throw new DonorImportException($"Could not understand donor type {input}");
            }
        }
    }
}