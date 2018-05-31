using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nova.DonorService.Client;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Repositories;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        Task StartDonorImport();

        void ImportSingleTestDonor();
        void ImportTenSolarDonors();
        void ImportDummyData();
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

        public void ImportSingleTestDonor()
        {
            donorRepository.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla
                    {
                        Locus = Locus.A,
                        PGroups = new List<string> { "01:01P" }
                    },
                    A_2 = new ExpandedHla
                    {
                        Locus = Locus.A,
                        PGroups = new List<string> { "01:01P" }
                    }
                }
            });
        }

        public void ImportTenSolarDonors()
        {
            foreach (RawInputDonor donor in solarRepository.SomeDonors(1000))
            {
                InsertSingleRawDonor(donor);
            }
        }

        private void InsertSingleRawDonor(RawInputDonor donor)
        {
            Enum.TryParse(donor.RegistryCode, out RegistryCode code);
            donorRepository.InsertDonor(new InputDonor
            {
                RegistryCode = code,
                DonorType = DonorTypeFromString(donor.DonorType),
                DonorId = donor.DonorId,
                MatchingHla = donor.HlaNames.Map((locus, position, hla) => lookupService.GetMatchingHla(locus.ToMatchLocus(), hla).Result.ToExpandedHla())
            });
        }

        // TODO:NOVA-919 extract to a spreadsheet-backed repository if this stays
        private string SpreadsheetContents()
        {
            Assembly assem = this.GetType().Assembly;
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Resources.DonorPhenotypesSample.csv"))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void ImportDummyData()
        {
            var spreadsheetDonors = Regex.Split(SpreadsheetContents(), "\r\n|\r|\n")
                .Skip(1) // Header row
                .Select(a => a.Split(','))
                .Select(a => a.Select(val => string.IsNullOrWhiteSpace(val) ? null : val).ToArray<string>())
                .Select((a, i) => new RawInputDonor
                {
                    DonorId = i+1, // Don't want donor ID 0
                    DonorType = a[1],
                    RegistryCode = a[0],
                    HlaNames = new PhenotypeInfo<string>
                    {
                        A_1 = a[2],
                        A_2 = a[3],
                        B_1 = a[4],
                        B_2 = a[5],
                        C_1 = a[6],
                        C_2 = a[7],
                        DQB1_1 = a[8],
                        DQB1_2 = a[9],
                        DRB1_1 = a[10],
                        DRB1_2 = a[11]
                    }
                });

            // TODO:NOVA-919 batch import
            foreach (RawInputDonor donor in spreadsheetDonors)
            {
                InsertSingleRawDonor(donor);
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