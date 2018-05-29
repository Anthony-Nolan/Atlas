using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.DonorService.Client;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        void ResumeDonorImport();

        void ImportSingleTestDonor();
        void ImportTenSolarDonors();
        void ImportDummyData();
    }
    static class DonorExtensions
    {
        public static RawInputDonor ToRawImportDonor(this Donor donor)
        {
            return new RawInputDonor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                HlaNames = new PhenotypeInfo<string>
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
                }
            };
        }
    }

    public class DonorImportService : IDonorImportService
    {
        private readonly IDonorMatchRepository donorRepository;
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly ISolarDonorRepository solarRepository;
        private readonly IDonorServiceClient donorServiceClient;
        
        public DonorImportService(
            IDonorMatchRepository donorRepository,
            IMatchingDictionaryLookupService lookupService,
            ISolarDonorRepository solarRepository,
            IDonorServiceClient donorServiceClient)
        {
            this.donorRepository = donorRepository;
            this.solarRepository = solarRepository;
            this.lookupService = lookupService;
            this.donorServiceClient = donorServiceClient;
        }

        public async void ResumeDonorImport()
        {
            // TODO:NOVA-1170 for now just import 10
            // TODO:NOVA-1170 update the donor client so that the second param can be omitted
            var page = await donorServiceClient.GetDonors(10, "0");
            foreach (var donor in page.Donors)
            {
                InsertSingleRawDonor(donor.ToRawImportDonor());
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
                DonorType = DonorType.Adult,
                DonorId = donor.DonorId,
                MatchingHla = donor.HlaNames.Map((locus, position, hla) => lookupService.GetMatchingHla(locus.ToMatchLocus(), hla).Result.ToExpandedHla())
            });
        }

        // TODO:NOVA-919 extract to a spreasheet-backed repository if this stays
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
    }
}