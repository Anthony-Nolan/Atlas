using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Repositories;

namespace Nova.SearchAlgorithm.Services
{
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

        public async Task ImportSolarDonors()
        {
            foreach (RawInputDonor donor in await solarRepository.SomeDonors(1000))
            {
                InsertSingleRawDonor(donor);
            }
        }

        public async Task ImportAllDonorsFromSolar()
        {
            var batchSize = 400;
            var lastId = 0;

            var batch = (await solarRepository.SomeDonors(batchSize)).ToList();

            while (batch.Any())
            {
                await Task.WhenAll(batch.Select(donorRepository.InsertDonor));

                lastId = batch.OrderByDescending(d => d.DonorId).First().DonorId;

                batch = (await solarRepository.SomeDonors(batchSize, lastId)).ToList();
            }
        }

        private void InsertSingleRawDonor(RawInputDonor donor)
        {
            donorRepository.AddOrUpdateDonor(new InputDonor
            {
                RegistryCode = donor.RegistryCode,
                DonorType = donor.DonorType,
                DonorId = donor.DonorId,
                MatchingHla = donor.HlaNames.Map((locus, position, hla) => lookupService.GetMatchingHla(locus.ToMatchLocus(), hla).Result.ToExpandedHla())
            });
        }

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
                    DonorId = i + 1, // Don't want donor ID 0
                    RegistryCode = DonorExtensions.RegistryCodeFromString(a[0]),
                    DonorType = DonorExtensions.DonorTypeFromString(a[1]),
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

            foreach (RawInputDonor donor in spreadsheetDonors)
            {
                InsertSingleRawDonor(donor);
            }
        }
    }
}