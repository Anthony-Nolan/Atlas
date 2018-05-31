using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Repositories;

namespace Nova.SearchAlgorithm.Services
{
    /// <summary>
    /// A service for inserting test data for the convenience of developers.
    /// TODO:NOVA-1151 remove this service before going into production
    /// </summary>
    public interface ITestDataService
    {
        void ImportSingleTestDonor();
        void ImportSolarDonors();
        void ImportDummyData();
    }

    public class TestDataService : ITestDataService
    {
        private readonly IDonorImportRepository donorRepository;
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly ISolarDonorRepository solarRepository;

        public TestDataService(
            IDonorImportRepository donorRepository,
            IMatchingDictionaryLookupService lookupService,
            ISolarDonorRepository solarRepository)
        {
            this.donorRepository = donorRepository;
            this.solarRepository = solarRepository;
            this.lookupService = lookupService;
        }

        public void ImportSingleTestDonor()
        {
            donorRepository.AddOrUpdateDonor(new InputDonor
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

        public void ImportSolarDonors()
        {
            foreach (RawInputDonor donor in solarRepository.SomeDonors(1000))
            {
                InsertSingleRawDonor(donor);
            }
        }

        private void InsertSingleRawDonor(RawInputDonor donor)
        {
            Enum.TryParse(donor.RegistryCode, out RegistryCode code);
            donorRepository.AddOrUpdateDonor(new InputDonor
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
                .Select(a => a.Select(val => string.IsNullOrWhiteSpace(val) ? null : val).ToArray())
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
                    throw new SearchHttpException($"Could not understand donor type {input}");
            }
        }
    }
}