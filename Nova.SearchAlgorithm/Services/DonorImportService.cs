using System;
using System.Linq;
using System.Threading.Tasks;
using Nova.DonorService.Client;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Repositories;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        Task StartDonorImport();
    }

    public class DonorImportService : IDonorImportService
    {
        // TODO:NOVA-1170 for now just import 10. Increase batch size later.
        private const int DonorPageSize = 10;

        private readonly IDonorMatchRepository donorRepository;
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly ISolarDonorRepository solarRepository;
        private readonly IDonorServiceClient donorServiceClient;
        private readonly ILogger logger;

        public DonorImportService(
            IDonorMatchRepository donorRepository,
            IMatchingDictionaryLookupService lookupService,
            ISolarDonorRepository solarRepository,
            IDonorServiceClient donorServiceClient,
            ILogger logger)
        {
            this.donorRepository = donorRepository;
            this.solarRepository = solarRepository;
            this.lookupService = lookupService;
            this.donorServiceClient = donorServiceClient;
            this.logger = logger;
        }

        public async Task StartDonorImport()
        {
            await ContinueDonorImport(donorRepository.HighestDonorId());
        }

        public async Task ContinueDonorImport(int lastId)
        {
            logger.SendTrace($"Requesting donor page size {DonorPageSize} from ID {lastId} onwards", LogLevel.Trace);

            var page = await donorServiceClient.GetDonors(DonorPageSize, lastId);

            if (page.Donors.Any())
            {
                // TODO:NOVA-1170: Insert in batches for efficiency
                foreach (var donor in page.Donors)
                {
                    donorRepository.InsertDonor(await ConvertRawDonor(donor));
                }

                var nextId = page.LastId ?? donorRepository.HighestDonorId();

                await ContinueDonorImport(nextId);
            }
            else
            {
                logger.SendTrace("Donor import complete", LogLevel.Info);
            }
        }

        private async Task<InputDonor> ConvertRawDonor(Donor donor)
        {
            return new InputDonor
            {
                RegistryCode = RegistryCodeFromString(donor.RegistryCode),
                DonorType = DonorTypeFromString(donor.DonorType),
                DonorId = donor.DonorId,
                MatchingHla = await LookupDonorHla(donor)
            };
        }

        private async Task<PhenotypeInfo<ExpandedHla>> LookupDonorHla(Donor donor)
        {
            return new PhenotypeInfo<ExpandedHla>
            {
                A_1 = await LookupMatchingHla(Locus.A, donor.A_1),
                A_2 = await LookupMatchingHla(Locus.A, donor.A_2),
                B_1 = await LookupMatchingHla(Locus.B, donor.B_1),
                B_2 = await LookupMatchingHla(Locus.B, donor.B_2),
                C_1 = await LookupMatchingHla(Locus.C, donor.C_1),
                C_2 = await LookupMatchingHla(Locus.C, donor.C_2),
                DQB1_1 = await LookupMatchingHla(Locus.Dqb1, donor.DQB1_1),
                DQB1_2 = await LookupMatchingHla(Locus.Dqb1, donor.DQB1_2),
                DRB1_1 = await LookupMatchingHla(Locus.Drb1, donor.DRB1_1),
                DRB1_2 = await LookupMatchingHla(Locus.Drb1, donor.DRB1_2)
            };
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
            // TODO:NOVA-1170 DonorImportException
            // TODO:NOVA-1170 Log exceptions and continue
            throw new SearchHttpException($"Could not understand registry code {input}");
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
                    // TODO:NOVA-1170 DonorImportException
                    // TODO:NOVA-1170 Log exceptions and continue
                    throw new SearchHttpException($"Could not understand donor type {input}");
            }
        }
    }
}