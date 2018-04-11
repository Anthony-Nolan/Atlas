using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hla;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorImportService
    {
        void ImportSingleTestDonor();
        void ImportTenSolarDonors();
        void ImportDummyData();
    }

    // TODO:NOVA-929 implement correctly
    public class DonorImportService : IDonorImportService
    {
        private readonly IDonorRepository donorRepository;
        private readonly IHlaRepository hlaRepository;
        private readonly ISolarDonorRepository solarRepository;

        public DonorImportService(IDonorRepository donorRepository, IHlaRepository hlaRepository, ISolarDonorRepository solarRepository)
        {
            this.donorRepository = donorRepository;
            this.solarRepository = solarRepository;
            this.hlaRepository = hlaRepository;
        }

        public void ImportSingleTestDonor()
        {
            donorRepository.InsertDonor(new ImportDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = "Adult",
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<MatchingHla>
                {
                    A_1 = new MatchingHla
                    {
                        Locus = "A",
                        Type = "Allele",
                        IsDeleted = false,
                        MatchingProteinGroups = new List<string> { "01:01P" },
                        MatchingSerologyNames = new List<string> { "1" }
                    },
                    A_2 = new MatchingHla
                    {
                        Locus = "A",
                        Type = "Allele",
                        IsDeleted = false,
                        MatchingProteinGroups = new List<string> { "01:01P" },
                        MatchingSerologyNames = new List<string> { "1" }
                    }
                }
            });
        }

        public void ImportTenSolarDonors()
        {
            foreach (RawDonor donor in solarRepository.SomeDonors(10))
            {
                InsertSingleRawDonor(donor);
            }
        }

        private void InsertSingleRawDonor(RawDonor donor)
        {
            Enum.TryParse(donor.RegistryCode, out RegistryCode code);
            donorRepository.InsertDonor(new ImportDonor
            {
                RegistryCode = code,
                DonorType = "Adult",
                DonorId = donor.DonorId,
                MatchingHla = donor.HlaNames.Map((locus, position, hla) => hlaRepository.RetrieveHlaMatches(locus, hla))
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
                .Select(a => a.Split(','))
                .Select(a => a.Select(val => string.IsNullOrWhiteSpace(val) ? null : val).ToArray<string>())
                .Select((a, i) => new RawDonor
                {
                    DonorId = i,
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
            foreach (RawDonor donor in spreadsheetDonors)
            {
                InsertSingleRawDonor(donor);
            }
        }
    }
}